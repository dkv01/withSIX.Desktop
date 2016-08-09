// <copyright company="SIX Networks GmbH" file="CalculatedGameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using MoreLinq;
using NDepend.Path;
using ReactiveUI;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Play.Core.Games.Entities;
using SN.withSIX.Play.Core.Games.Entities.RealVirtuality;
using SN.withSIX.Play.Core.Games.Legacy.Events;
using SN.withSIX.Play.Core.Games.Legacy.Missions;
using SN.withSIX.Play.Core.Games.Legacy.Mods;
using SN.withSIX.Play.Core.Options.Entries;
using AsyncLock = SN.withSIX.Core.Helpers.AsyncLock;
using PropertyChangedBase = SN.withSIX.Core.Helpers.PropertyChangedBase;

namespace SN.withSIX.Play.Core.Games.Legacy
{
    [Obsolete("In process of removal")]
    public class CalculatedGameSettings : PropertyChangedBase, IEnableLogging
    {
        public static Action<object> RaiseEvent;
        readonly string[] _arma2CompatibilityPacks = {
            "@AllInArmaStandalone", "@AllInArmaStandaloneLite",
            "@PWS_EnableA2OAContentInA3"
        };
        readonly Game _game;
        readonly ISupportModding _modding;
        readonly bool _supportsMissions;
        readonly bool _supportsModding;
        readonly bool _supportsServers;

        private readonly AsyncLock subgLock = new AsyncLock();
        Collection _collection;
        IList<Mod> _currentMods = new List<Mod>();
        //IList<Mod> _allCurrentMods = new List<Mod>();
        bool _first;
        [Obsolete("", true)] IAbsoluteDirectoryPath _gamePath;
        bool _ignoreUpdate;
        MissionBase _mission;
        MissionBase _parkedMission;
        Server _parkedServer;
        bool _raiseEvent;
        Server _server;

        public CalculatedGameSettings(Game game) {
            _game = game;
            _supportsModding = _game.SupportsMods();
            _supportsMissions = _game.SupportsMissions();
            _supportsServers = _game.SupportsServers();
            if (_supportsModding)
                _modding = _game.Modding();

            Signatures = new string[0];
            if (!Execute.InDesignMode) {
                var collectionChanged = this.WhenAnyValue(x => x.Collection)
                    .Skip(1);
                collectionChanged
                    .Subscribe(HandleModSetSwitch);

                // TODO: Ignore out of band responses, cancel previous etc...
                collectionChanged
                    .ObserveOn(ThreadPoolScheduler.Instance)
                    .Subscribe(x => UpdateSignatures());

                this.WhenAnyValue(x => x.Server)
                    .Skip(1)
                    .Subscribe(HandleServerSwitch);

                this.WhenAnyValue(x => x.Server.Mods)
                    .Skip(1)
                    .Subscribe(HandleServerModsChanged);

                this.WhenAnyValue(x => x.Mission)
                    .Skip(1)
                    .Subscribe(HandleMissionSwitch);
            }

            _first = true;
        }

        [Obsolete("HACK")]
        public static IContentManager ContentManager { get; set; }
        public string[] Signatures { get; private set; }
        public bool InitialSynced { get; set; }
        IAbsoluteDirectoryPath ModPath { get; set; }
        IAbsoluteDirectoryPath SynqPath { get; set; }
        IAbsoluteDirectoryPath GamePath
        {
            get { return _gamePath; }
            set
            {
                if (!SetProperty(ref _gamePath, value))
                    return;
                if (_raiseEvent)
                    RaiseEvent(new GamePathChanged(value));
            }
        }
        public Collection Collection
        {
            get { return _collection; }
            set { SetProperty(ref _collection, value); }
        }
        public Server Server
        {
            get { return _server; }
            set { SetProperty(ref _server, value); }
        }
        public Server Queued { get; set; }
        public MissionBase Mission
        {
            get { return _mission; }
            set { SetProperty(ref _mission, value); }
        }
        public OverallUpdateState State { get; set; }
        public IList<Mod> CurrentMods
        {
            get { return _currentMods; }
            private set { SetProperty(ref _currentMods, value); }
        }
        [Obsolete("Legacy to support arma2 terrain packs")]
        public bool HasArma2TerrainPacks { get; private set; }
        [Obsolete("Legacy to support allinarma")]
        public bool HasAllInArmaLegacy { get; private set; }
        [Obsolete("Legacy to support allinarma standalone")]
        public bool HasArma2CompatibilityPack { get; private set; }
        public static Func<object, Task> NotifyEnMass { get; set; }

        public void UpdateSignatures() {
            var ms = Collection;
            Signatures = ms?.Items == null
                ? new string[0]
                : GetSignatures(ms).ToArray();
        }

        public bool UpdateMod(bool raiseEvent = true) {
            var modsChanged = false;
            if (_supportsModding) {
                var previousMods = _currentMods; //_allCurrentMods;
                //var allCurrentMods = GetAllMods().ToList();
                var currentMods = GetMods().ToList();
                modsChanged = !previousMods.Select(x => x.Name).SequenceEqual(/*allCurrentMods*/ currentMods.Select(x => x.Name));
                //_allCurrentMods = allCurrentMods;
                CurrentMods = currentMods;
            }

            if (!modsChanged)
                return false;

            HandleSubG();

            if (!raiseEvent)
                return modsChanged;

            RaiseEvent(new CalculatedGameSettingsUpdated(true, modsChanged));

            return true;
        }

        async void HandleSubG() {
            await Task.Factory.StartNew(async () => {
                using (await subgLock.LockAsync().ConfigureAwait(false))
                    await HandleSubGames().ConfigureAwait(false);
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
        }

        IEnumerable<Mod> GetMods() => HandleMods(CollectionCompatibleEnabledMods().ToArray());
        //IEnumerable<Mod> GetAllMods() => HandleMods(CollectionEnabledMods().ToArray());

        private IEnumerable<Mod> HandleMods(IReadOnlyCollection<Mod> collectionMods) {
            IEnumerable<Mod> currentMods = collectionMods;
            var server = Server;
            if (_supportsServers && ServerModsEnabled() && server != null)
                currentMods = currentMods.Concat(ServerMods(server));
            var mission = Mission;
            if (_supportsMissions && mission != null)
                currentMods = currentMods.Concat(mission.GetMods(_modding, ContentManager));

            return Collection.CleanupAiaCup(collectionMods, currentMods.Distinct()
                .DistinctBy(x => x.Name?.ToLower()).ToArray());
        }

        async Task HandleSubGames() {
            var a3Tp = HasArma2TerrainPacks;
            var aia = HasAllInArmaLegacy;
            var aiaLite = HasArma2CompatibilityPack;
            var currentMods = _currentMods;//_allCurrentMods;
            var packageNames = currentMods.Select(x => x.PackageName);
            HasArma2TerrainPacks =
                Arma3Game.Arma2TerrainPacks.Any(m => packageNames.ContainsIgnoreCase(m));
            HasAllInArmaLegacy = packageNames.ContainsIgnoreCase("@AllInArma");
            HasArma2CompatibilityPack =
                _arma2CompatibilityPacks.Any(a => packageNames.ContainsIgnoreCase(a));
            if (a3Tp != HasArma2TerrainPacks || aia != HasAllInArmaLegacy || aiaLite != HasArma2CompatibilityPack)
                await NotifyEnMass(new SubGamesChanged(_game)).ConfigureAwait(false);
        }


        public void Update() {
            _raiseEvent = true;
            if (_first) {
                _raiseEvent = false;
                var installedState = _game.InstalledState;
                _gamePath = installedState.IsInstalled ? installedState.Directory : null;

                if (_supportsModding) {
                    if (installedState.IsInstalled) {
                        var paths = _modding.ModPaths;
                        ModPath = paths.Path;
                        SynqPath = paths.RepositoryPath;
                    }
                }
                _first = false;
            }

            SetGamePath();

            var modsChanged = false;
            if (_supportsModding) {
                SetModPath();
                SetSynqPath();
                modsChanged = UpdateMod(false);
            }

            if (_raiseEvent)
                RaiseEvent(new CalculatedGameSettingsUpdated(false, modsChanged));
            Info();
        }

        void Info() {
            var installedState = _game.InstalledState;
            this.Logger().Info(
                "CalculatedGameSettings for {6}: Path: {0}, ExePath: {1}, LaunchExePath: {2}, GameVersion: {3}, ModPath: {4}, SynqPath: {5}",
                Tools.FileUtil.FilterPath(GamePath), Tools.FileUtil.FilterPath(installedState.Executable),
                Tools.FileUtil.FilterPath(installedState.LaunchExecutable), installedState.Version,
                Tools.FileUtil.FilterPath(ModPath), Tools.FileUtil.FilterPath(SynqPath), _game.MetaData.Name);
        }

        public void SwitchToServer() {
            var modSet = Collection;
            _ignoreUpdate = true;
            try {
                if (modSet == null)
                    _parkedMission = Mission;
                else
                    modSet.ParkedMission = Mission;
                Mission = null;

                Server = modSet == null ? _parkedServer : modSet.ParkedServer;
            } finally {
                _ignoreUpdate = false;
            }
            UpdateMod();
        }

        public void SwitchToMission() {
            _ignoreUpdate = true;
            try {
                var modSet = Collection;
                if (modSet == null)
                    _parkedServer = Server;
                else
                    modSet.ParkedServer = Server;
                Server = null;

                Mission = modSet == null ? _parkedMission : modSet.ParkedMission;
            } finally {
                _ignoreUpdate = false;
            }
            UpdateMod();
        }

        public RecentMission GetRecentMission() {
            var modSet = Collection;
            return modSet == null ? _game.Settings.Recent.Mission : modSet.RecentMission;
        }

        public RecentServer GetRecentServer() {
            var modSet = Collection;
            return modSet == null ? _game.Settings.Recent.Server : modSet.RecentServer;
        }

        void HandleModSetSwitch(Collection value) {
            _ignoreUpdate = true;
            try {
                HandleServer();
                HandleMission();
            } finally {
                _ignoreUpdate = false;
            }
            UpdateRecentModSet(value);
            UpdateMod();
        }

        void HandleMission() {
            var recentMission = GetRecentMission();
            if (recentMission == null) {
                Mission = null;
                return;
            }

            Mission = ContentManager.Missions.FirstOrDefault(recentMission.Matches); // TODO: local
        }

        void HandleServer() {
            var recentServer = GetRecentServer();
            if (recentServer == null) {
                Server = null;
                return;
            }

            Server = ContentManager.ServerList.FindOrCreateServer(recentServer.Address);
        }

        void HandleMissionSwitch(MissionBase value) {
            if (_ignoreUpdate)
                return;
            UpdateRecentMission(value);
            UpdateMod();
        }

        void HandleServerSwitch(Server value) {
            if (_ignoreUpdate)
                return;
            UpdateRecentServer(value);
            UpdateMod();
        }

        void HandleServerModsChanged(string[] value) {
            if (_ignoreUpdate)
                return;
            UpdateMod();
        }

        void UpdateRecentMission(MissionBase value) {
            var recentMission = value != null && value.ObjectTag != null ? new RecentMission(value) : null;

            var ms = Collection;
            if (ms != null) {
                ms.RecentMission = recentMission;
                DomainEvilGlobal.Settings.RaiseChanged();
            } else {
                var gs = _game;
                gs.Settings.Recent.Mission = recentMission;
            }
        }

        void UpdateRecentModSet(Collection value) {
            var gs = _game;
            gs.Settings.Recent.Collection = value == null || value.Id == Guid.Empty ? null : new RecentCollection(value);
        }

        void UpdateRecentServer(Server value) {
            var recentServer = value != null ? new RecentServer(value) : null;
            var ms = Collection;
            if (ms != null) {
                ms.RecentServer = recentServer;
                DomainEvilGlobal.Settings.RaiseChanged();
            } else {
                var gs = _game;
                gs.Settings.Recent.Server = recentServer;
            }
        }

        bool ServerModsEnabled() {
            var modSet = Collection;

            if (!_game.SupportsServers())
                return false;

            var armaSettings = _game.Settings as ArmaSettings;
            if (armaSettings == null)
                return false;
            return armaSettings.IncludeServerMods &&
                   (modSet == null || !modSet.SkipServerMods);
        }

        static IEnumerable<string> GetSignatures(Collection ms) => ms.EnabledMods.Select(x => x.Controller)
    .Where(x => x != null && x.Exists)
    .SelectMany(GetSignaturesSafe)
    .Distinct();

        static IEnumerable<string> GetSignaturesSafe(ModController modController) {
            try {
                return modController.GetSignatures();
            } catch (UnauthorizedAccessException e) {
                MainLog.Logger.FormattedWarnException(e, "while processing signatures");
                return Enumerable.Empty<string>();
            }
        }

        bool ChangeModPath(IAbsoluteDirectoryPath value) {
            if (ModPath != null && ModPath.Equals(value))
                return false;

            ModPath = value;
            return true;
        }

        //IEnumerable<Mod> CollectionEnabledMods() => Collection == null ? Enumerable.Empty<Mod>() : Collection.GetAllEnabledMods();
        IEnumerable<Mod> CollectionCompatibleEnabledMods() => Collection == null ? Enumerable.Empty<Mod>() : Collection.GetAllCompatibleEnabledMods();

        // TODO: THIS NEEDS OPTIMIZATION. WE DONT WANT TO READ FROM DISK HERE, and deifnitely dont want to download!!
        // Low prio because missions are really not used that much atm...

        IEnumerable<Mod> ServerMods(Server server) {
            if (!server.Mods.Any())
                return Enumerable.Empty<Mod>();

            var customModSet = Collection as AdvancedCollection;
            return GetServerMods(customModSet, server);
        }

        IEnumerable<Mod> GetServerMods(AdvancedCollection customCollection, Server server) {
            if (!server.Mods.Any())
                return Enumerable.Empty<Mod>();
            return customCollection != null
                ? GetCustomRepoServerMods(server, customCollection)
                : GetServerMods(server);
        }

        IEnumerable<Mod> GetServerMods(Server server) => ContentManager.GetMods(_modding, server.Mods);

        IEnumerable<Mod> GetCustomRepoServerMods(Server server, AdvancedCollection ms) => ContentManager.GetMods(_modding, server.Mods, ms.CustomRepoMods);

        void SetGamePath() {
            var installedState = _game.InstalledState;
            GamePath = installedState.IsInstalled ? installedState.Directory : null;
        }

        void SetModPath() {
            var paths = _modding.ModPaths;
            var mp = ModPath;
            if (ChangeModPath(paths.Path)
                && _raiseEvent)
                RaiseEvent(new ModPathChanged(ModPath, mp));
        }

        void SetSynqPath() {
            var paths = _modding.ModPaths;
            var sp = SynqPath;
            if (ChangeSynqPath(paths.RepositoryPath)
                && _raiseEvent)
                RaiseEvent(new SynqPathChanged(SynqPath, sp));
        }

        bool ChangeSynqPath(IAbsoluteDirectoryPath value) {
            if (SynqPath != null && SynqPath.Equals(value))
                return false;

            SynqPath = value;
            return true;
        }
    }
}