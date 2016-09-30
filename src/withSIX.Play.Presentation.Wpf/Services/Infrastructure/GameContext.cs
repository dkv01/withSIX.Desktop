// <copyright company="SIX Networks GmbH" file="GameContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;

using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Infrastructure;
using SN.withSIX.Core.Applications.MVVM.Extensions;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Play.Applications.Services.Infrastructure;
using SN.withSIX.Play.Core;
using SN.withSIX.Play.Core.Games.Entities;
using SN.withSIX.Play.Core.Games.Entities.Other;
using SN.withSIX.Play.Core.Games.Entities.RealVirtuality;
using SN.withSIX.Play.Core.Games.Legacy;
using SN.withSIX.Play.Core.Games.Legacy.Missions;
using SN.withSIX.Play.Core.Games.Legacy.Mods;
using SN.withSIX.Play.Core.Games.Legacy.Repo;
using SN.withSIX.Play.Core.Options;
using SN.withSIX.Play.Core.Options.Entries;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Play.Presentation.Wpf.Services.Infrastructure
{
    
    public class GameContext : IGameContext, IInfrastructureService
    {
        readonly Lazy<InMemoryDbSet<Collection, Guid>> _collections;
        readonly Lazy<InMemoryDbSet<CustomCollection, Guid>> _customCollections;
        readonly Lazy<ReactiveList<SixRepo>> _customRepositories;
        readonly Lazy<InMemoryDbSet<Game, Guid>> _games;
        readonly GameSettingsController _gameSettingsController;
        readonly Lazy<ReactiveList<LocalMissionsContainer>> _localMissionsContainers;
        readonly Lazy<ReactiveList<LocalModsContainer>> _localModsContainers;
        readonly Lazy<InMemoryDbSet<Mission, Guid>> _missions;
        readonly Lazy<InMemoryDbSet<Mod, Guid>> _mods;
        readonly Lazy<InMemoryDbSet<SubscribedCollection, Guid>> _subscribedCollections;

        public GameContext(UserSettings settings) {
            _gameSettingsController = settings.GameOptions.GameSettingsController;
            _games = new Lazy<InMemoryDbSet<Game, Guid>>(CreateDbSet);
            _mods = new Lazy<InMemoryDbSet<Mod, Guid>>(() => CreateDbSet(new ReactiveList<Mod>()));

            _localMissionsContainers = new Lazy<ReactiveList<LocalMissionsContainer>>(() => {
                var src = settings.MissionOptions.LocalMissions;
                lock (src) {
                    var source = new ReactiveList<LocalMissionsContainer>(src);
                    foreach (var lm in source)
                        lm.Game = Games.Find(lm.GameId);
                    source.KeepCollectionInSync(src);
                    return source;
                }
            });

            _localModsContainers = new Lazy<ReactiveList<LocalModsContainer>>(() => {
                var src = settings.ModOptions.LocalMods;
                lock (src) {
                    var source = new ReactiveList<LocalModsContainer>(src);
                    foreach (var lm in source)
                        lm.Game = Games.Find(lm.GameId);
                    source.KeepCollectionInSync(src);
                    return source;
                }
            });

            _customRepositories = new Lazy<ReactiveList<SixRepo>>(() => {
                var src = settings.ModOptions.Repositories;
                lock (src) {
                    var source = new ReactiveList<SixRepo>(src.Values);
                    source.TrackChanges(x => {
                        lock (src)
                            src[x.Uri.ToString()] = x;
                    },
                        x => {
                            lock (src)
                                src.Remove(x.Uri.ToString());
                        },
                        reset => {
                            lock (src) {
                                foreach (var r in reset)
                                    src[r.Uri.ToString()] = r;
                                foreach (var r in src.Where(x => !reset.Contains(x.Value)))
                                    src.Remove(r.Key);
                            }
                        });
                    //source.KeepCollectionInSync(settings.ModOptions.Repositories.Values);
                    return source;
                }
            });

            _missions = new Lazy<InMemoryDbSet<Mission, Guid>>(
                () => {
                    var set = CreateDbSet(new ReactiveList<Mission>());
                    ProcessMissions(set);
                    return set;
                });

            _collections =
                new Lazy<InMemoryDbSet<Collection, Guid>>(
                    () => {
                        var set = CreateDbSet(new ReactiveList<Collection>());
                        ProcessCollections(set);
                        return set;
                    });

            _customCollections =
                new Lazy<InMemoryDbSet<CustomCollection, Guid>>(
                    () => {
                        var src = settings.ModOptions.CustomCollections;
                        lock (src) {
                            var customCollectionsSource =
                                new ReactiveList<CustomCollection>(src);
                            customCollectionsSource.KeepCollectionInSync(src);
                            var set = CreateDbSet(customCollectionsSource);
                            ProcessCollections(set);
                            return set;
                        }
                    });
            _subscribedCollections =
                new Lazy<InMemoryDbSet<SubscribedCollection, Guid>>(
                    () => {
                        var src = settings.ModOptions.SubscribedCollections;
                        lock (src) {
                            var subscribedCollectionsSource =
                                new ReactiveList<SubscribedCollection>(src);
                            subscribedCollectionsSource.KeepCollectionInSync(src);
                            var set = CreateDbSet(subscribedCollectionsSource);
                            ProcessCollections(set);
                            return set;
                        }
                    });
        }

        public IDbSet<Collection, Guid> Collections => _collections.Value;
        public ReactiveList<LocalMissionsContainer> LocalMissionsContainers => _localMissionsContainers.Value;
        public ReactiveList<LocalModsContainer> LocalModsContainers => _localModsContainers.Value;
        public ReactiveList<SixRepo> CustomRepositories => _customRepositories.Value;

        public void ImportCollections(ICollection<Collection> collections) {
            ProcessCollections(collections);
            lock (Collections.Local)
                collections.SyncCollectionPK(Collections.Local);
        }

        public void ImportMissions(ICollection<Mission> missions) {
            ProcessMissions(missions);
            lock (Missions.Local)
                missions.SyncCollectionPK(Missions.Local);
        }

        public void ImportMods(ICollection<Mod> mods) {
            lock (Mods.Local)
                mods.SyncCollectionPK(Mods.Local);
        }

        public IDbSet<SubscribedCollection, Guid> SubscribedCollections => _subscribedCollections.Value;
        public IDbSet<Mission, Guid> Missions => _missions.Value;
        public IDbSet<Mod, Guid> Mods => _mods.Value;
        public IDbSet<CustomCollection, Guid> CustomCollections => _customCollections.Value;
        public IDbSet<Game, Guid> Games => _games.Value;

        public Task<int> SaveChanges() {
            throw new NotImplementedException();
        }

        void ProcessMissions(IEnumerable<Mission> missions) {
            var defaultGame = GetDefaultGame();
            foreach (var mission in missions) {
                var game = Games.FirstOrDefault(x => x.Id == mission.GameId);
                mission.Game = game ?? defaultGame;
            }
        }

        void ProcessCollections(IEnumerable<Collection> bla) {
            var defaultGame = GetDefaultGame();

            foreach (var collection in bla) {
                var game = Games.FirstOrDefault(x => x.Id == collection.GameId && x.SupportsMods());
                collection.ChangeGame(game ?? defaultGame);
            }
        }

        Game GetDefaultGame() => Games.First(x => x.Id == GameGuids.Arma2Co);

        InMemoryDbSet<T, Guid> CreateDbSet<T>(ReactiveList<T> input) where T : IHaveId<Guid>
            => new InMemoryDbSet<T, Guid>(input);

        InMemoryDbSet<Game, Guid> CreateDbSet() {
            var list = new List<Game>();
            CreateRealVirtualityGames(list);
            CreateOtherGames(list);

            foreach (var g in list)
                g.Initialize();

            // This executes after processing all games, because CO imports OA settings when missing etc...
            DomainEvilGlobal.Settings.GameOptions.ClearLegacyGameSettings();

            return new InMemoryDbSet<Game, Guid>(new ReactiveList<Game>(list));
        }

        void CreateRealVirtualityGames(ICollection<Game> list) {
            CreateArmaGames(list);

            list.Add(new DayZGame(GameGuids.DayZ, _gameSettingsController));
        }

        void CreateArmaGames(ICollection<Game> list) {
            var arma1Game = new Arma1Game(GameGuids.Arma1, _gameSettingsController);
            list.Add(arma1Game);

            var arma2FreeGame = new Arma2FreeGame(GameGuids.Arma2Free, _gameSettingsController);
            list.Add(arma2FreeGame);

            var arma2Game = new Arma2Game(GameGuids.Arma2, _gameSettingsController);
            list.Add(arma2Game);

            var arma2OaGame = new Arma2OaGame(GameGuids.Arma2Oa, _gameSettingsController);
            list.Add(arma2OaGame);

            list.Add(new Arma2COGame(GameGuids.Arma2Co, _gameSettingsController, arma2Game, arma2FreeGame));

            list.Add(new IronFrontGame(GameGuids.IronFront, _gameSettingsController));

            var takeOnHelicoptersGame = new TakeOnHelicoptersGame(GameGuids.TakeOnHelicopters, _gameSettingsController);
            list.Add(takeOnHelicoptersGame);

            list.Add(new Arma3Game(GameGuids.Arma3, _gameSettingsController,
                new Arma3Game.AllInArmaGames(arma1Game, arma2Game, arma2FreeGame, arma2OaGame, takeOnHelicoptersGame)));
        }

        void CreateOtherGames(ICollection<Game> list) {
#if DEBUG
            list.Add(new Homeworld2Game(GameGuids.Homeworld2, _gameSettingsController));
#endif
            list.Add(new CarrierCommandGame(GameGuids.CarrierCommand, _gameSettingsController));
            list.Add(new TakeOnMarsGame(GameGuids.TakeOnHelicopters, _gameSettingsController));
#if DEBUG
            list.Add(new KerbalSPGame(GameGuids.KerbalSP, _gameSettingsController));
            list.Add(new GTAVGame(GameGuids.GTA5, _gameSettingsController));
#endif
        }
    }
}