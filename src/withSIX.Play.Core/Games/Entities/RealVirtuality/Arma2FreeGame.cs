// <copyright company="SIX Networks GmbH" file="Arma2FreeGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using ReactiveUI;
using withSIX.Core.Extensions;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Missions;
using withSIX.Play.Core.Games.Legacy.ServerQuery;
using withSIX.Play.Core.Games.Services;
using withSIX.Play.Core.Options;
using withSIX.Play.Core.Options.Entries;
using withSIX.Play.Core.Options.Filters;

namespace withSIX.Play.Core.Games.Entities.RealVirtuality
{
    public class Arma2FreeGame : RealVirtualityGame, ISupportMissions, ISupportProfiles, ISupportServers
    {
        static readonly SteamInfo steamInfo = new SteamInfo(107400, "Arma 2 Free");
        static readonly SeparateClientAndServerExecutable executables =
            new SeparateClientAndServerExecutable("arma2free.exe", "arma2server.exe");
        static readonly RegistryInfo registryInfo = new RegistryInfo(BohemiaStudioRegistry + @"\ArmA 2 Free", "main");
        static readonly RvProfileInfo profileInfo = new RvProfileInfo("Arma 2", "Arma 2 other profiles", "Arma2Profile");
        static readonly GamespyServersQuery serverQueryInfo = new GamespyServersQuery("arma2pc");
        static readonly GameMetaData metaData = new GameMetaData {
            Name = "ARMA 2 Original (Free)",
            ShortName = "ARMA II (Free)",
            Author = "Bohemia Interactive",
            Slug = "arma-2",
            Description =
                @"Arma 2: Free (A2F) redefines the free-to-play battlefield with its truly unrivalled scale and gameplay possibilities. A2F serves up almost everything offered by the original Arma 2 - the '13th best PC game of all time', according to PC Gamer [1] - minus the campaign, HD graphics and support for user-made addons and mods.
        Create your own custom-built scenarios or deploy a massive selection of missions and game-modes made by others. No micro-transactions, no hidden costs, just the same epic terrain and huge variety of equipment! This is Arma 2 Free - virtual war without the training wheels.
        Arma 2: Free to download, free to play, free to share, free to host, free to create... free to play redefined!",
            ReleasedOn = new DateTime(2008, 1, 1),
            IsFree = true
        };
        ContentPaths[] _missionPaths;

        protected Arma2FreeGame(Guid id, Arma2FreeSettings settings)
            : base(id, settings) {
            Settings = settings;
        }

        public Arma2FreeGame(Guid id, GameSettingsController settingsController)
            : this(id, new Arma2FreeSettings(id, new ArmaStartupParams(DefaultStartupParameters), settingsController)) {}

        protected override SteamInfo SteamInfo => steamInfo;
        public new Arma2FreeSettings Settings { get; }
        protected override RegistryInfo RegistryInfo => registryInfo;
        public override GameMetaData MetaData => metaData;
        protected override RvProfileInfo ProfileInfo => profileInfo;
        protected virtual SeparateClientAndServerExecutable Executables => executables;
        protected override bool IsClient => !Settings.StartupParameters.Server && GetExecutable().FileName == Executables.Client;
        public ContentPaths PrimaryContentPath => MissionPaths.FirstOrDefault();

        public void PublishMission(string fn) {
            PublishMissionInternal(fn);
        }

        public virtual bool SupportsContent(Mission mission) => mission.ContentType == GameMissionType.Arma2Mission;

        public ContentPaths[] MissionPaths
        {
            get { return _missionPaths ?? (_missionPaths = GetMissionPaths(Settings.RepositoryDirectory)); }
            private set { SetProperty(ref _missionPaths, value); }
        }

        public IEnumerable<LocalMissionsContainer> LocalMissionsContainers() => GetLocalMissionsContainers();

        public void UpdateMissionStates(IReadOnlyCollection<MissionBase> missions) {
            foreach (var m in missions)
                m.Controller.UpdateState(this);
        }

        public IEnumerable<string> GetProfiles() => GetProfilesInternal();

        public virtual Task<IEnumerable<ServerQueryResult>> QueryServers(IGameServerQueryHandler queryHandler) => queryHandler.Query(serverQueryInfo);

        public Task QueryServer(ServerQueryState state) => serverQueryInfo.QueryServer(state);

        public IFilter GetServerFilter() => Settings.ServerFilter;

        public bool SupportsServerType(string type) => serverQueryInfo.Tag == type;

        public Server CreateServer(ServerAddress address) => new Arma2FreeServer(this, address);

        protected override IAbsoluteFilePath GetExecutable() => GetExe(GetGameDirectory(), Executables, Settings.ServerMode);

        public override void Initialize() {
            base.Initialize();
            this.WhenAnyValue(x => x.InstalledState)
                .Skip(1)
                .Subscribe(x => UpdateMissionPaths());
            Settings.WhenAnyValue(x => x.RepositoryDirectory)
                .Skip(1)
                .Subscribe(x => UpdateMissionPaths());

            Settings.WhenAnyValue(x => x.ServerMode, x => x.StartupParameters.Server, (x, y) => x || y)
                .Skip(1)
                .Subscribe(x => RefreshState());
        }

        public override void RefreshState() {
            UpdateInstalledState();
            UpdateMissionPaths();
            CalculatedSettings.Update();
        }

        void UpdateMissionPaths() {
            MissionPaths = GetMissionPaths(Settings.RepositoryDirectory);
        }
    }

    public class Arma2FreeSettings : RealVirtualitySettings
    {
        public Arma2FreeSettings(Guid gameId, ArmaStartupParams startupParameters, GameSettingsController controller)
            : base(gameId, startupParameters, controller) {
            StartupParameters = startupParameters;
            if (ServerFilter == null)
                ServerFilter = new ArmaServerFilter();
        }

        public new ArmaStartupParams StartupParameters { get; }
        public IAbsoluteDirectoryPath RepositoryDirectory
        {
            get { return GetValue<string>().ToAbsoluteDirectoryPathNullSafe(); }
            set { SetValue(value == null ? null : value.ToString()); }
        }
        public ArmaServerFilter ServerFilter
        {
            get { return GetValue<ArmaServerFilter>(); }
            set { SetValue(value); }
        }
        public bool ServerMode
        {
            get { return GetBoolValue(); }
            set { SetBoolValue(value); }
        }
    }
}