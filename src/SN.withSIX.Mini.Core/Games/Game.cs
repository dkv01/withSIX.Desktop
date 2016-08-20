// <copyright company="SIX Networks GmbH" file="Game.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MoreLinq;
using NDepend.Path;
using SN.withSIX.ContentEngine.Core;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Extensions;
using SN.withSIX.Mini.Core.Games.Attributes;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller.Attributes;
using SN.withSIX.Mini.Core.Games.Services.GameLauncher;
using SN.withSIX.Steam.Core;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Core.Games
{
    public interface IHaveSourcePaths
    {
        ContentPaths ContentPaths { get; }
        SteamWorkshopDirectories SteamworkshopPaths { get; }
    }

    [DataContract]
    public abstract class Game : BaseEntity<Guid>, IContentEngineGame, IHaveSourcePaths
    {
        internal static readonly SteamHelper SteamHelper = new SteamHelper(new SteamStuff().TryReadSteamConfig(),
            SteamStuff.GetSteamPath());

        static readonly string[] getCompatibilityMods = new string[0];

        private static readonly Guid[] steamGames = {GameGuids.Starbound, GameGuids.Stellaris, GameGuids.Skyrim};
        private readonly List<Guid> _compatibleGameIds;

        private readonly Lazy<SteamDirectories> _steamDirectories;
        Lazy<ContentPaths> _contentPaths;
        Lazy<GameInstalledState> _installedState;

        protected Game(Guid id, GameSettings settings) {
            Id = id;
            Settings = settings;

            Metadata = this.GetMetaData<GameAttribute>();
            RemoteInfo = this.GetMetaData<RemoteInfoAttribute>();
            RegistryInfo = this.GetMetaData(RegistryInfoAttribute.Default);
            SteamInfo = this.GetMetaData(SteamInfoAttribute.Default);
            ContentCleaning = this.GetMetaData(ContentCleaningAttribute.Default);

            LastUsedLaunchType = Metadata.LaunchTypes.First();

            _installedState = new Lazy<GameInstalledState>(GetInstalledState);
            _contentPaths = new Lazy<ContentPaths>(GetContentPathsState);
            _compatibleGameIds = Enumerable.Repeat(Id, 1).ToList();
            _steamDirectories = SystemExtensions.CreateLazy(() => SteamInfo.GetDirectories(SteamHelper));

            if (!DefaultDirectoriesOverriden)
                SetupDefaultDirectories();
        }

        protected SteamDirectories SteamDirectories => _steamDirectories.Value;

        // We use this because of chicken-egg problems because of constructor inheritance load order
        // Where usually overriden behavior depends on state that is not yet available in the base class constructor
        protected virtual bool DefaultDirectoriesOverriden => false;
        [IgnoreDataMember]
        protected ContentCleaningAttribute ContentCleaning { get; }
        [IgnoreDataMember]
        protected RemoteInfoAttribute RemoteInfo { get; }
        [IgnoreDataMember]
        public SteamInfoAttribute SteamInfo { get; }
        [IgnoreDataMember]
        protected RegistryInfoAttribute RegistryInfo { get; }
        [DataMember]
        public GameSettings Settings { get; protected set; }
        [IgnoreDataMember]
        public GameAttribute Metadata { get; }
        [IgnoreDataMember]
        public IEnumerable<LocalContent> LocalContent => Contents.OfType<LocalContent>();
        [IgnoreDataMember]
        public IEnumerable<Content> InstalledContent => Contents.Where(x => x.IsInstalled());
        [IgnoreDataMember]
        public IEnumerable<Content> IncompleteContent => Contents.Where(x => x.IsIncompleteInstalled());
        [IgnoreDataMember]
        public IEnumerable<NetworkContent> NetworkContent => Contents.OfType<NetworkContent>();
        [DataMember]
        public virtual ICollection<Content> Contents { get; protected set; } = new List<Content>();
        [IgnoreDataMember]
        public IEnumerable<Collection> Collections => Contents.OfType<Collection>();
        [IgnoreDataMember]
        public IEnumerable<SubscribedCollection> SubscribedCollections => Contents.OfType<SubscribedCollection>();
        [IgnoreDataMember]
        public IEnumerable<LocalCollection> LocalCollections => Contents.OfType<LocalCollection>();
        [IgnoreDataMember]
        public IEnumerable<NetworkContent> Updates
            => InstalledContent.Where(x => x.GetState() == ItemState.UpdateAvailable)
                .OfType<NetworkContent>();

        [IgnoreDataMember]
        public IEnumerable<Content> AllAvailableContent => InstalledContent.Concat(IncompleteContent);

        [DataMember]
        public DateTime? LastPlayed { get; set; }
        [DataMember]
        public bool FirstTimeRunShown { get; set; }
        [IgnoreDataMember]
        public GameInstalledState InstalledState => _installedState.Value;
        [DataMember]
        public LaunchType LastUsedLaunchType { get; set; }
        [IgnoreDataMember]
        public IEnumerable<Content> RecentItems => Contents.Where(x => x.RecentInfo != null);

        [DataMember]
        public SyncInfo SyncInfo { get; protected set; } = new SyncInfo();

        private IAbsoluteDirectoryPath ExecutablePath => InstalledState.Executable.ParentDirectoryPath;
        // TODO: we could also choose to implement this as a wrapper/adapter class instead
        IAbsoluteDirectoryPath IContentEngineGame.WorkingDirectory => InstalledState.WorkingDirectory;

        public SteamWorkshopDirectories SteamworkshopPaths => SteamDirectories.Workshop;
        [IgnoreDataMember]
        public ContentPaths ContentPaths => _contentPaths.Value;

        protected virtual IAbsoluteDirectoryPath GetContentDirectory() => InstalledState.WorkingDirectory;

        public IAbsoluteDirectoryPath GetContentPath(IContentWithPackageName content) {
            ConfirmInstalled();
            return content.GetSourceDirectory(this);
        }

        public IAbsoluteDirectoryPath GetContentPath() {
            ConfirmInstalled();
            // TODO: When steam game, should provide the option to select which folder to open?!
            return ContentPaths.Path;
        }

        public IAbsoluteDirectoryPath GetPath() {
            ConfirmInstalled();
            return InstalledState.Directory;
        }

        public async Task RefreshState() {
            if (InstalledState.IsInstalled && ContentPaths.IsValid)
                await ScanForLocalContent().ConfigureAwait(false);
            else
                ResetStates();
        }

        public Task ScanForLocalContent() => ScanForLocalContentInternal();

        async Task ScanForLocalContentInternal() {
            ConfirmInstalled();
            await ScanForLocalContentImpl().ConfigureAwait(false);
            RefreshCollections();
        }

        protected abstract Task ScanForLocalContentImpl();

        ContentPaths GetContentPathsState() {
            if (!InstalledState.IsInstalled)
                return ContentPaths.Default;
            var contentDir = GetContentDirectory();
            if (contentDir == null)
                return ContentPaths.Default;

            var repoDir = GetRepoDirectory();
            if (repoDir == null)
                return ContentPaths.Default;

            return new ContentPaths(contentDir, repoDir);
        }

        public virtual IReadOnlyCollection<Guid> GetCompatibleGameIds() => _compatibleGameIds;

        public virtual IReadOnlyCollection<string> GetCompatibilityMods(string packageName,
            IReadOnlyCollection<string> tags) => getCompatibilityMods;

        void SetupDefaultDirectories() {
            if (Settings.GameDirectory == null)
                Settings.GameDirectory = GetDefaultDirectory();
            if (Settings.RepoDirectory == null && Settings.GameDirectory != null)
                Settings.RepoDirectory = Settings.GameDirectory;
        }

        GameInstalledState GetInstalledState() {
            var gameDirectory = GetGameDirectory();
            if (gameDirectory == null)
                return GameInstalledState.Default;
            var executable = GetExecutable();
            if (!executable.Exists)
                return GameInstalledState.Default;
            var launchExecutable = GetLaunchExecutable();
            if (!launchExecutable.Exists)
                return GameInstalledState.Default;
            return new GameInstalledState(executable, launchExecutable, gameDirectory, GetWorkingDirectory());
        }

        // TODO: Get this path from somewhere else than global state!!
        protected IAbsoluteDirectoryPath GetGameLocalDataFolder()
            => Common.Paths.LocalDataPath.GetChildDirectoryWithName("games")
                .GetChildDirectoryWithName(Id.ToShortId().ToString());

        // TODO: Make this part of a domain event call?
        public void RefreshCollections() {
            foreach (var c in Collections)
                c.UpdateState(false);
        }

        public void UseContent(IContentAction<IContent> action) {
            var content = HandleContentAction(action);
            action.Use(content);
        }

        private IContent HandleContentAction(IContentAction<IContent> action) {
            var content = action.Content.Count == 1
                ? action.Content.First().Content
                : FindOrCreateLocalCollection(action);
            action.Name = content.Name;
            // TODO: it would probably be better to have rewritten the action content, so we dont again create a temporary collection further down the line..
            return content;
        }

        LocalCollection FindOrCreateLocalCollection(IContentAction<IContent> action) {
            var contents = action.Content.Select(x => new ContentSpec((Content) x.Content, x.Constraint)).ToList();
            var existing =
                Collections.OfType<LocalCollection>().FirstOrDefault(x => x.Contents.SequenceEqual(contents));
            if (existing != null)
                return existing;
            var isNotUpdateAll = action.Name != "Update all" && action.Name != "Available updates";
            var name = isNotUpdateAll
                ? $"{action.Name ?? "Playlist"} {DateTime.UtcNow.ToString(Tools.GenericTools.DefaultDateFormat)}"
                : action.Name;
            var localCollection = new LocalCollection(Id, name, contents);
            if (isNotUpdateAll)
                Contents.Add(localCollection);
            return localCollection;
        }

        protected void AddInstalledContent(params Content[] installedContent) {
            Contract.Requires<ArgumentNullException>(installedContent != null);
            Contract.Requires<ArgumentOutOfRangeException>(installedContent.Any());
            Contents.AddRange(installedContent);
            PrepareEvent(new ContentInstalled(Id, installedContent));
        }

        public Task<int> Play(IGameLauncherFactory factory, IContentInstallationService contentInstallation,
            IPlayContentAction<LocalContent> action) => PlayInternal(factory, contentInstallation, action);

        public Task<int> Play(IGameLauncherFactory factory,
            IContentInstallationService contentInstallation, IPlayContentAction<Content> action)
            => PlayInternal(factory, contentInstallation, action);

        public async Task Install(IContentInstallationService installationService,
            DownloadContentAction installAction) {
            await InstallInternal(installationService, installAction).ConfigureAwait(false);
            PrepareEvent(new InstallActionCompleted(installAction, this));
        }

        public async Task Uninstall(IContentInstallationService contentInstallation,
            IUninstallContentAction<IUninstallableContent> uninstallLocalContentAction) {
            await UninstallInternal(contentInstallation, uninstallLocalContentAction).ConfigureAwait(false);
            PrepareEvent(new UninstallActionCompleted(uninstallLocalContentAction, this));
        }

        // TODO: Enter content into LAUNCHING state, then once completed, back to the content's state.
        public async Task<int> Launch(IGameLauncherFactory factory,
            ILaunchContentAction<Content> launchContentAction) {
            var pid = await LaunchInternal(factory, launchContentAction).ConfigureAwait(false);
            PrepareEvent(new LaunchActionCompleted(this, pid));
            return pid;
        }

        async Task<int> PlayInternal(IGameLauncherFactory factory, IContentInstallationService contentInstallation,
            IPlayContentAction<IContent> action) {
            ConfirmPlay();
            await InstallInternal(contentInstallation, action.ToInstall()).ConfigureAwait(false);
            return await LaunchInternal(factory, action).ConfigureAwait(false);
        }

        Task InstallInternal(IContentInstallationService contentInstallation,
            IDownloadContentAction<IInstallableContent> installAction) {
            ConfirmInstall();
            return InstallImpl(contentInstallation, installAction);
        }

        async Task UninstallInternal(IContentInstallationService contentInstallation,
            IUninstallContentAction<IUninstallableContent> uninstallContentAction) {
            //ConfirmUninstall()
            await UninstallImpl(contentInstallation, uninstallContentAction).ConfigureAwait(false);

            foreach (var c in uninstallContentAction.Content.Select(x => x.Content)
                .OfType<LocalContent>())
                Contents.Remove(c);
        }

        async Task<int> LaunchInternal(IGameLauncherFactory factory, ILaunchContentAction<IContent> action) {
            ConfirmLaunch();

            await BeforeLaunch(action).ConfigureAwait(false);

            int id;
            using (var p = await LaunchImpl(factory, action).ConfigureAwait(false))
                id = p?.Id ?? -1;
            LastPlayed = Tools.Generic.GetCurrentUtcDateTime;
            PrepareEvent(new GameLaunched(this, id));

            if (Metadata.AfterLaunchDelay.HasValue)
                await Task.Delay(Metadata.AfterLaunchDelay.Value, action.CancelToken).ConfigureAwait(false);

            return id;
        }

        protected virtual async Task BeforeLaunch(ILaunchContentAction<IContent> action) {}

        protected abstract Task<Process> LaunchImpl(IGameLauncherFactory factory,
            ILaunchContentAction<IContent> launchContentAction);

        protected abstract Task InstallImpl(IContentInstallationService installationService,
            IDownloadContentAction<IInstallableContent> downloadContentAction);

        protected abstract Task UninstallImpl(IContentInstallationService contentInstallation,
            IContentAction<IUninstallableContent> uninstallLocalContentAction);

        protected virtual void ConfirmPlay() {
            ConfirmInstalled();
            ConfirmNotRunning();
        }

        protected virtual void ConfirmInstall() {
            ConfirmInstalled();
            ConfirmNotRunning();
            ConfirmContentPaths();
        }

        protected virtual void ConfirmLaunch() {
            ConfirmInstalled();
        }

        void ConfirmNotRunning() {
            if (IsRunning())
                throw new GameIsRunningException(Metadata.Name + " is already running");
        }

        protected void ConfirmInstalled() {
            if (!InstalledState.IsInstalled)
                throw new GameNotInstalledException(Metadata.Name + " is not found");
        }

        void ConfirmContentPaths() {
            if (!ContentPaths.IsValid)
                throw new InvalidPathsException("Invalid content target directories");
        }

        protected virtual IAbsoluteDirectoryPath GetWorkingDirectory() => GetExecutable().ParentDirectoryPath;

        protected virtual IAbsoluteFilePath GetExecutable() {
            var executables = GetExecutables();
            var path = executables.Select(GetFileInGameDirectory).FirstOrDefault(p => p.Exists);
            return path ?? GetFileInGameDirectory(executables.First());
        }

        protected virtual string[] GetExecutables() => Metadata.Executables;

        protected virtual IAbsoluteFilePath GetLaunchExecutable() => GetExecutable();

        IAbsoluteDirectoryPath GetRepoDirectory() => Settings.RepoDirectory;

        IAbsoluteFilePath GetFileInGameDirectory(string file) => GetGameDirectory().GetChildFileWithName(file);

        IAbsoluteDirectoryPath GetGameDirectory() => Settings.GameDirectory;

        protected virtual IAbsoluteDirectoryPath GetDefaultDirectory()
            => RegistryInfo.TryGetDefaultDirectory() ?? SteamInfo.TryGetDefaultDirectory();

        protected virtual bool IsLaunchingSteamApp() {
            var gameDir = InstalledState.Directory;
            var steamApp = SteamInfo.TryGetSteamApp();
            if (steamApp.IsValid) {
                return gameDir.Equals(steamApp.AppPath) ||
                       InstalledState.LaunchExecutable.ParentDirectoryPath.DirectoryInfo.EnumerateFiles("steam_api*.dll")
                           .Any();
            }
            return false;
        }

        protected virtual Task<LaunchGameInfo> GetDefaultLaunchInfo(IEnumerable<string> startupParameters)
            => Task.FromResult(new LaunchGameInfo(InstalledState.LaunchExecutable, InstalledState.Executable,
                InstalledState.WorkingDirectory,
                startupParameters) {
                    LaunchAsAdministrator = ShouldLaunchAsAdministrator()
                });

        protected virtual Task<LaunchGameWithSteamInfo> GetSteamLaunchInfo(IEnumerable<string> startupParameters)
            => Task.FromResult(new LaunchGameWithSteamInfo(InstalledState.LaunchExecutable, InstalledState.Executable,
                InstalledState.WorkingDirectory,
                startupParameters) {
                    SteamAppId = SteamInfo.AppId,
                    SteamDRM = SteamInfo.DRM,
                    LaunchAsAdministrator = ShouldLaunchAsAdministrator()
                });

        protected bool ShouldLaunchAsAdministrator() => Settings.LaunchAsAdministrator.GetValueOrDefault();

        bool IsRunning() => GetRunningInstances().Any();

        // TODO: Optimize
        private IEnumerable<Tuple<Process, IAbsoluteFilePath>> GetRunningInstances()
            => Metadata.Executables.SelectMany(x => Tools.Processes.GetExecuteablePaths(x))
                .Where(x => x != null && (x.Item2 == null || ExecutablePath.Equals(x.Item2.ParentDirectoryPath)));

        public async Task UpdateSettings(GameSettings settings) {
            Settings = settings;

            // We refresh the info already here because we are in background thread..
            var installedState = GetInstalledState();
            _installedState = new Lazy<GameInstalledState>(() => installedState);
            var contentPathState = GetContentPathsState();
            _contentPaths = new Lazy<ContentPaths>(() => contentPathState);

            // TODO: Make this visible in UI somehow..
            // TODO: Perhaps make this a background operation?
            // TODO: Only scan when the paths have actually changed..

            await RefreshState().ConfigureAwait(false);

            PrepareEvent(new GameSettingsUpdated(this));
        }

        private void ResetStates() {
            foreach (var c in Contents)
                c.Uninstalled();
        }

        public string GetContentPath(IHavePath content) => Metadata.Slug + "/" + content.GetPath();

        public void RemoveCollection(Collection collection) {
            collection.Uninstalled(); // we want the content status to update
            Contents.Remove(collection);
        }

        public void ClearRecent() {
            foreach (var r in RecentItems.ToArray())
                r.RemoveRecentInfo();
        }

        public virtual IAbsoluteDirectoryPath GetConfigPath() {
            throw new NotImplementedException();
        }

        public virtual IAbsoluteDirectoryPath GetConfigPath(IPackagedContent content) {
            throw new NotImplementedException();
        }

        public void Close() {
            ConfirmInstalled();
            GetRunningInstances().ForEach(x => x.Item1.TryKill());
        }

        public bool IsSteamGame() => steamGames.Contains(Id);

        protected IAbsoluteDirectoryPath GetContentSourceDirectory(IContentWithPackageName content)
            => content.GetSourceDirectory(this);
    }

    public class ApiHashes : global::withSIX.Api.Models.Content.v3.ApiHashes {}

    [DataContract]
    public class SyncInfo
    {
        [DataMember]
        public DateTime LastSync { get; set; }
        [DataMember]
        public int LastSyncVersion { get; set; }
        [DataMember]
        public ApiHashes ApiHashes { get; set; }
    }

    public class GameTerminated : ISyncDomainEvent
    {
        public GameTerminated(Game game, int processId) {
            Game = game;
            ProcessId = processId;
        }

        public Game Game { get; }
        public int ProcessId { get; }
    }

    public class LaunchActionCompleted : ISyncDomainEvent
    {
        public LaunchActionCompleted(Game game, int pid) {
            Game = game;
        }

        public Game Game { get; }
    }

    public class UninstallActionCompleted : ISyncDomainEvent
    {
        public UninstallActionCompleted(IUninstallContentAction<IUninstallableContent> uninstallLocalContentAction,
            Game game) {
            UninstallLocalContentAction = uninstallLocalContentAction;
            Game = game;
        }

        public IUninstallContentAction<IUninstallableContent> UninstallLocalContentAction { get; }
        public Game Game { get; }
    }

    public class InstallActionCompleted : ISyncDomainEvent
    {
        public InstallActionCompleted(DownloadContentAction action, Game game) {
            Action = action;
            Game = game;
        }

        public Game Game { get; }
        public DownloadContentAction Action { get; }
    }

    public class GameSettingsUpdated : ISyncDomainEvent
    {
        public GameSettingsUpdated(Game game) {
            Game = game;
        }

        public Game Game { get; }
    }

    public class InvalidPathsException : InvalidOperationException
    {
        public InvalidPathsException(string message) : base(message) {}
    }

    public class GameIsRunningException : InvalidOperationException
    {
        public GameIsRunningException(string message) : base(message) {}
    }

    public class GameNotInstalledException : InvalidOperationException
    {
        public GameNotInstalledException(string message) : base(message) {}
    }

    public class ContentUsed : ISyncDomainEvent
    {
        public ContentUsed(Content content, IContentAction<IContent> action) {
            Content = content;
            Action = action;
        }

        public Content Content { get; }
        public IContentAction<IContent> Action { get; }
    }

    public class ContentInstalled : ISyncDomainEvent
    {
        public ContentInstalled(Guid gameId, params IContent[] contentInstalled) {
            GameId = gameId;
            Content = contentInstalled.ToList();
        }

        public Guid GameId { get; }
        public List<IContent> Content { get; }
    }

    public class GameLaunched : TimestampedDomainEvent
    {
        public GameLaunched(Game game, int processId) {
            Game = game;
            ProcessId = processId;
        }

        public Game Game { get; }
        public int ProcessId { get; }
    }

    public class GameRequirementMissingException : Exception
    {
        public GameRequirementMissingException() {}
        public GameRequirementMissingException(string message) : base(message) {}
    }

    public class MultiGameRequirementMissingException : GameRequirementMissingException
    {
        public MultiGameRequirementMissingException(IReadOnlyCollection<GameRequirementMissingException> exceptions) {
            Exceptions = exceptions;
        }

        public IReadOnlyCollection<GameRequirementMissingException> Exceptions { get; }
    }
}