// <copyright company="SIX Networks GmbH" file="SynqInstallerSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using ReactiveUI;
using withSIX.Api.Models.Exceptions;
using SN.withSIX.ContentEngine.Core;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Mini.Core.Social;
using SN.withSIX.Steam.Api;
using SN.withSIX.Steam.Core.SteamKit.DepotDownloader;
using SN.withSIX.Sync.Core;
using SN.withSIX.Sync.Core.Legacy.SixSync.CustomRepo;
using SN.withSIX.Sync.Core.Legacy.Status;
using SN.withSIX.Sync.Core.Packages;
using SN.withSIX.Sync.Core.Repositories;
using SN.withSIX.Sync.Core.Transfer;
using withSIX.Api.Models;
using withSIX.Api.Models.Content;
using CheckoutType = SN.withSIX.Mini.Core.Games.CheckoutType;

namespace SN.withSIX.Mini.Applications.Services
{
    public class SynqInstallerSession : IInstallerSession
    {
        readonly IInstallContentAction<IInstallableContent> _action;
        private readonly IAuthProvider _authProvider;

        private AverageContainer2 _averageSpeed;
        readonly IContentEngine _contentEngine;
        readonly Func<bool> _getIsPremium;
        readonly List<Tuple<IPackagedContent, string>> _installed = new List<Tuple<IPackagedContent, string>>();
        readonly List<SpecificVersion> _installedContent = new List<SpecificVersion>();
        //private readonly ProgressLeaf _preparingProgress;
        private readonly ProgressComponent _progress;
        readonly Func<ProgressInfo, Task> _statusChange;
        private readonly StatusRepo _statusRepo = new StatusRepo(); // we do this for abort capability
        readonly IToolsCheat _toolsInstaller;
        private Dictionary<IPackagedContent, SpecificVersion> _allContentToInstall;
        private Dictionary<IContent, string> _allInstallableContent = new Dictionary<IContent, string>();
        private Dictionary<IPackagedContent, SpecificVersion> _groupContent = new Dictionary<IPackagedContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _groupContentToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private SixSyncProgress _groupProgress;
        private IReadOnlyCollection<Group> _groups = new List<Group>();
        private Dictionary<IPackagedContent, SpecificVersion> _installableContent;
        private Dictionary<IPackagedContent, SpecificVersion> _packageContent =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private PackageProgress _packageProgress;
        private Dictionary<IPackagedContent, SpecificVersion> _packagesToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private IAbsoluteDirectoryPath _packPath;
        PackageManager _pm;
        private Dictionary<IPackagedContent, SpecificVersion> _steamContent = new Dictionary<IPackagedContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _steamContentToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private ProgressComponent _steamProgress;
        private Dictionary<IPackagedContent, SpecificVersion> _repoContent = new Dictionary<IPackagedContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _repoContentToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private SixSyncProgress _repoProgress;
        private IAbsoluteDirectoryPath _repoPath;
        IReadOnlyCollection<CustomRepo> _repositories = new List<CustomRepo>();
        Repository _repository;
        private readonly SteamDepotDownloader _steamDepotDownloader;

        public SynqInstallerSession(IInstallContentAction<IInstallableContent> action, IToolsCheat toolsInstaller, Func<bool> isPremium, Func<ProgressInfo, Task> statusChange, IContentEngine contentEngine, IAuthProvider authProvider, ISteamDownloader steamDownloader, IDbContextLocator contextLocator) {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (toolsInstaller == null)
                throw new ArgumentNullException(nameof(toolsInstaller));
            if (statusChange == null)
                throw new ArgumentNullException(nameof(statusChange));
            _action = action;
            _toolsInstaller = toolsInstaller;
            _getIsPremium = isPremium;
            _statusChange = statusChange;
            _contentEngine = contentEngine;
            _authProvider = authProvider;
            _progress = new ProgressComponent("Stage");
            _averageSpeed = new AverageContainer2(20);
            //_progress.AddComponents(_preparingProgress = new ProgressLeaf("Preparing"));
            _steamDepotDownloader = new SteamDepotDownloader(steamDownloader, contextLocator);

        }

        public async Task Install(IReadOnlyCollection<IContentSpec<IPackagedContent>> content) {
            PrepareContent(content);
            await PublishIndividualItemStates().ConfigureAwait(false);

            var completed = false;
            try {
                await TryInstallContent().ConfigureAwait(false);
                completed = true;
            } finally {
                if (completed)
                    MarkContentAsFinished();
                else
                    MarkContentAsUnfinished();
            }
        }

        public async Task Synchronize() {
            // TODO: Install Tools as part of the Setup process instead? Also include in Setup package..
            await InstallToolsIfNeeded().ConfigureAwait(false);
            //_preparingProgress.Progress = 10;
            CreateDirectories();
            await PerformInstallation().ConfigureAwait(false);
        }

        public void Abort() => _statusRepo.Abort();

        public void RunCE(IPackagedContent content) {
            if (_contentEngine.ModHasScript(content.Id)) {
                _contentEngine.LoadModS(new ContentEngineContent(content.Id, content.Id, true,
                    _action.Paths.Path.GetChildDirectoryWithName(content.PackageName),
                    content.GameId)).processMod();
            }
        }

        private async Task PerformInstallation() {
            using (_statusRepo)
            using (
                Observable.Interval(TimeSpan.FromMilliseconds(500))
                    .SelectMany(x => Observable.FromAsync(TryStatusChange))
                    .Subscribe()) {
                await PrepareGroupsAndRepositories().ConfigureAwait(false);
                // TODO: make it unneeded to initialize Synq stuff unless we actually need it..
                if (_action.RemoteInfo != null) {
                    using (_repository = new Repository(_repoPath, true)) {
                        SetupPackageManager();
                        await UpdateSynqRemotes().ConfigureAwait(false);
                        await InstallContent().ConfigureAwait(false);
                    }
                } else {
                    await InstallContent().ConfigureAwait(false);
                }
            }
        }

        private async Task PrepareGroupsAndRepositories() {
            //_preparingProgress.Progress = 50;
            await HandleGroups().ConfigureAwait(false);
            //_preparingProgress.Progress = 70;
            await HandleRepositories().ConfigureAwait(false);
        }

        async Task TryStatusChange() {
            try {
                await StatusChange().ConfigureAwait(false);
            } catch (Exception ex) {
                MainLog.Logger.FormattedWarnException(ex, "Error sending status update");
            }
        }

        Task StatusChange()
            => _statusChange(new ProgressInfo(_progress.StatusText, _progress.Progress, _progress.Flatten(),
                _averageSpeed.UpdateSpecial(_progress.Speed)));

        async Task TryLegacyStatusChange(double progress, long? speed) {
            try {
                await LegacyStatusChange(progress, speed).ConfigureAwait(false);
            } catch (Exception ex) {
                MainLog.Logger.FormattedWarnException(ex, "Error sending legacy status update");
            }
        }

        Task LegacyStatusChange(double progress, long? speed)
            => _statusChange(new ProgressInfo(_progress.StatusText, progress, _progress.Flatten(),
                _averageSpeed.UpdateSpecial(speed)));

        private void CreateDirectories() {
            _repoPath = _action.Paths.RepositoryPath.GetChildDirectoryWithName(Repository.DefaultRepoRootDirectory);
            _packPath = _repoPath.GetChildDirectoryWithName("legacy");

            Tools.FileUtil.Ops.CreateDirectoryAndSetACLWithFallbackAndRetry(_action.Game.InstalledState.Directory);
            if (_action.GlobalWorkingPath != null)
                Tools.FileUtil.Ops.CreateDirectoryAndSetACLWithFallbackAndRetry(_action.GlobalWorkingPath);
            Tools.FileUtil.Ops.CreateDirectoryAndSetACLWithFallbackAndRetry(_action.Paths.Path);
            Tools.FileUtil.Ops.CreateDirectoryAndSetACLWithFallbackAndRetry(_action.Paths.RepositoryPath);
        }

        private void PrepareContent(IEnumerable<IContentSpec<IPackagedContent>> content) {
            // Let's prevent us from reinstalling the same packages over and over (even if it's fairly quick to check the files..)
            // TODO: Consider if instead we want to Concat up the Content, for a later all-at-once execution instead...
            // but then it would be bad to have an Install command on Content class, but not actually finish installing before the method ends
            // so instead we should have a Query on content then.
            _installableContent = content.ToDictionary(x => x.Content, x => new SpecificVersion(x.Content.GetFQN(x.Constraint)))
                .Where(x => !_installedContent.Contains(x.Value))
                .ToDictionary(x => x.Key, x => x.Value);

            // Add collections if available
            _allInstallableContent =
                _installableContent.ToDictionary(x => (IContent) x.Key, x => x.Value.VersionData)
                    .Concat(_action.Content.ToDictionary(x => (IContent) x.Content,
                        x => x.Constraint ?? x.Content.Version))
                    .Distinct()
                    .ToDictionary(x => x.Key, x => x.Value);
            PrepareGroupContent();
            PrepareRepoContent();
            PrepareSteamContent();
            PreparePackageContent();
            _allContentToInstall =
                _packagesToInstall.Concat(_repoContentToInstall)
                    .Concat(_groupContentToInstall)
                    .Concat(_steamContentToInstall)
                    .ToDictionary(x => x.Key, x => x.Value);
            //_preparingProgress.Finish();
        }

        private void PrepareSteamContent() {
            _steamContent =
                _installableContent
                    .Except(_groupContent)
                    .Except(_repoContent)
                    .Select(x => new {Content = x.Key as INetworkContent, x.Value})
                    .Where(x => ShouldInstallFromSteam(x.Content))
                    .ToDictionary(x => x.Content as IPackagedContent, y => y.Value);
            _steamContentToInstall = _steamContent; // TODO
            if (_steamContentToInstall.Any()) {
                _steamProgress = new ProgressComponent("Steam mods");
                _steamProgress.AddComponents(_steamContentToInstall.Select(x => new ProgressLeaf(x.Key.Name)).ToArray());
                _progress.AddComponents(_steamProgress);
            }
        }

        private static bool ShouldInstallFromSteam(INetworkContent content)
            => content.Publishers.Any(p => p.Publisher == Publisher.Steam) &&
               content.Publishers.All(x => x.Publisher != Publisher.withSIX);

        private void PrepareGroupContent() {
            _groupContent = _installableContent.Where(x => _groups.Any(r => r.HasMod(x.Value.Name)))
                .ToDictionary(x => x.Key, x => x.Value);
            _groupContentToInstall = _action.Force ? _groupContent : GetGroupContentToInstallOrUpdate();
            _groupProgress = SetupSixSyncProgress("Group mods", _groupContentToInstall);
        }

        private void PrepareRepoContent() {
            _repoContent = _installableContent.Except(_groupContent)
                .Where(x => _repositories.Any(r => r.HasMod(x.Value.Name)))
                .ToDictionary(x => x.Key, x => x.Value);
            _repoContentToInstall = _action.Force ? _repoContent : GetRepoContentToInstallOrUpdate();
            _repoProgress = SetupSixSyncProgress("Repo mods", _repoContentToInstall);
        }

        private void PreparePackageContent() {
            _packageContent = _installableContent
                .Except(_groupContent)
                .Except(_repoContent)
                .Except(_steamContent)
                .ToDictionary(x => x.Key, x => x.Value);
            if (_action.RemoteInfo == null) {
                if (_packageContent.Any()) throw new NotSupportedException("This game currently does not support SynqPackages");
                _packagesToInstall = _packageContent;
            } else {
                var remotePackageIndex = _pm.GetPackagesAsVersions(true);
                // TODO: Only handle content that is not yet on the right version (SynqInfo/RepositoryYml etc) Unless forced / diagnose?
                // The problem with GTA is that we wipe the folders beforehand.. Something we would solve with new mod folder approach
                _packagesToInstall = _action.Force ? _packageContent : OnlyWhenNewOrUpdateAvailable();
                HandleStats();
                _packageProgress = SetupSynqProgress("Network mods", _packagesToInstall);
            }
        }

        private SixSyncProgress SetupSixSyncProgress(string title,
            Dictionary<IPackagedContent, SpecificVersion> progressContent) {
            if (!progressContent.Any())
                return null;

            var modComponents = new ProgressComponent(title, progressContent.Count);
            var processingComponent = new ProgressComponent("Downloading");
            processingComponent.AddComponents(progressContent.Select(x => new ProgressLeaf(x.Key.Name)).ToArray());
            modComponents.AddComponents(processingComponent);
            _progress.AddComponents(modComponents);
            return new SixSyncProgress(modComponents, processingComponent);
        }

        private PackageProgress SetupSynqProgress(string title,
            Dictionary<IPackagedContent, SpecificVersion> progressContent) {
            if (!progressContent.Any())
                return null;

            var bla = PackageManager.SetupSynqProgress(title);

            var modComponents = new ProgressComponent(title, progressContent.Count);
            modComponents.AddComponents(bla.PackageFetching, bla.Processing, bla.Cleanup);
            _progress.AddComponents(modComponents);

            return bla;
        }

        async Task PublishIndividualItemStates(double progress = 0, long? speed = null) {
            // TODO: Combine status updates into single change?
            foreach (var c in _allContentToInstall) {
                await
                    new ContentStatusChanged(c.Key, GetProcessState(c.Key, c.Value.VersionData), progress, speed)
                        .Raise()
                        .ConfigureAwait(false);
            }
        }

        private ItemState GetProcessState(IContent content, string constraint) {
            if (_action.Force)
                return ItemState.Diagnosing;
            var processState = content.GetState(constraint);
            switch (processState) {
            case ItemState.NotInstalled:
                return ItemState.Installing;
            case ItemState.UpdateAvailable:
                return ItemState.Updating;
            case ItemState.Incomplete:
                return ItemState.Installing;
            }
            return processState;
        }

        // TODO: Rethink our strategy here. We convert it to a temp collection so that we can install all the content at once.
        // this is because we relinquish control to the content.Install method, and that will build the dependency tree and call the installer actions with it..
        Task InstallContent() => InstallContent(ConvertToTemporaryCollectionSpec());

        IContentSpec<IInstallableContent> ConvertToTemporaryCollectionSpec() => _action.Content.Count == 1
            ? _action.Content.First()
            : new InstallContentSpec(ConvertToTemporaryCollection());

        LocalCollection ConvertToTemporaryCollection() => new LocalCollection(_action.Game.Id, "$$temp",
            _action.Content.Select(x => new ContentSpec((Content) x.Content, x.Constraint)).ToList());

        async Task UpdateSynqRemotes() {
            await
                RepositoryHandler.ReplaceRemotes(GetRemotes(_action.RemoteInfo, _getIsPremium()), _repository)
                    .ConfigureAwait(false);
            await _pm.UpdateRemotes().ConfigureAwait(false);
            _pm.StatusRepo.Reset(RepoStatus.Processing, 0);
        }

        async Task TryInstallContent() {
            await InstallPackages().ConfigureAwait(false);
            await InstallSteamContent().ConfigureAwait(false);
            _averageSpeed = new AverageContainer2(20);
            using (new RepoWatcher(_statusRepo))
            using (new StatusRepoMonitor(_statusRepo, TryLegacyStatusChange)) {
                await InstallGroupContent().ConfigureAwait(false);
                await InstallRepoContent().ConfigureAwait(false);
            }
            await PerformPostInstallTasks(_installableContent).ConfigureAwait(false);
        }

        private void MarkContentAsUnfinished() {
            // TODO: How about marking this content at the start, much like .Use() for RecentItems
            // then even if the user restarts the computer / terminates the app, the state is preserved.
            // TODO: Minus the _installed content... however, they are not fully installed anyway until their postinstall tasks have completed..
            foreach (var cInfo in _allInstallableContent)
                cInfo.Key.Installed(cInfo.Value, false);
            _action.Game.RefreshCollections();
        }

        private void MarkContentAsFinished() {
            foreach (var cInfo in _allInstallableContent)
                cInfo.Key.Installed(cInfo.Value, true);
            _action.Game.RefreshCollections();
        }

        async Task PerformPostInstallTasks(Dictionary<IPackagedContent, SpecificVersion> installableContent) {
            foreach (var c in installableContent)
                await
                    c.Key.PostInstall(this, _action.CancelToken, _installedContent.Contains(c.Value))
                        .ConfigureAwait(false);
        }

        Task InstallToolsIfNeeded() => _toolsInstaller.SingleToolsInstallTask();

        async Task InstallGroupContent() {
            if (_groupContentToInstall.Count == 0)
                return;
            var contentProgress =
                _groupProgress.Processing
                    .GetComponents()
                    .OfType<ProgressLeaf>()
                    .ToArray();
            var i = 0;
            foreach (var cInfo in _groupContentToInstall)
                await InstallGroupC(cInfo.Value, cInfo.Key, contentProgress[i++]).ConfigureAwait(false);
            _installedContent.AddRange(_groupContentToInstall.Values);
            HandleInstallUpdateStats(_groupContentToInstall);
        }

        private void HandleInstallUpdateStats(Dictionary<IPackagedContent, SpecificVersion> contentToInstall) {
            foreach (var c in contentToInstall.Where(x => x.Key.GetState(x.Value.VersionData) == ItemState.NotInstalled))
                HandleInstallStats(c.Key);
            foreach (var c in contentToInstall.Where(x => x.Key.GetState(x.Value.VersionData) == ItemState.UpdateAvailable))
                HandleUpdateStats(c.Key);
        }

        private async Task InstallGroupC(SpecificVersion dep, IPackagedContent c, ProgressLeaf progressComponent) {
            var group = _groups.First(x => x.HasMod(dep.Name));
            var modInfo = @group.GetMod(dep.Name);
            await @group.GetMod(modInfo, _action.Paths.Path, _packPath, _pm.StatusRepo, _authProvider, _action.Force)
                .ConfigureAwait(false);
            _installed.Add(Tuple.Create(c, modInfo.Version));
            progressComponent.Finish();
            // TODO: Incremental info update, however this is hard due to implementation of SixSync atm..
        }

        async Task InstallSteamContent() {
            if (_steamContentToInstall.Count == 0)
                return;
            var i = 0;
            var contentProgress =
                _steamProgress
                    .GetComponents()
                    .OfType<ProgressLeaf>()
                    .ToArray();
            await HandleSteamSession().ConfigureAwait(false);
            foreach (var cInfo in _steamContentToInstall)
                await InstallSteam(cInfo.Value, (INetworkContent)cInfo.Key, contentProgress[i++]).ConfigureAwait(false);
            _installedContent.AddRange(_steamContentToInstall.Values);
            HandleInstallUpdateStats(_steamContentToInstall);
        }

        private async Task HandleSteamSession() {
            var appId = _action.Game.GetMetaData<SteamInfoAttribute>().AppId;
            if (Cheat.SteamSession == null)
                Cheat.SteamSession = await SteamSession.Start(appId).ConfigureAwait(false);
            else if (Cheat.SteamSession.AppId != appId)
                throw new InvalidOperationException(
                    "Steam was already initialized for another Game, currently you will have to restart the program to continue..");
        }

        private Task InstallSteam(SpecificVersion value, INetworkContent key, IUpdateSpeedAndProgress progress)
            => DownloadViaSteamClient(key, progress);

        private Task DownloadViaSteamClient(INetworkContent key, IUpdateSpeedAndProgress progress) {
            var appId = _action.Game.GetMetaData<SteamInfoAttribute>().AppId;
            var publisherId = key.Publishers.First(x => x.Publisher == Publisher.Steam).PublisherId;
            return new SteamDownloader().Download(appId, Convert.ToUInt64(publisherId), progress.Update,
                _action.CancelToken);
        }

        private Task DownloadViaDepot(INetworkContent key, ITProgress progress) {
            var publisherId = key.Publishers.First(x => x.Publisher == Publisher.Steam).PublisherId;
            var destination = _action.Paths.Path.GetChildDirectoryWithName(key.PackageName);
            destination.MakeSurePathExists();
            return
                _steamDepotDownloader.PerformSteamDepotDownloaderAction(progress, Convert.ToUInt64(publisherId),
                    destination,
                    _action.CancelToken);
        }

        async Task InstallRepoContent() {
            if (_repoContentToInstall.Count == 0)
                return;
            var contentProgress =
                _repoProgress.Processing
                    .GetComponents()
                    .OfType<ProgressLeaf>()
                    .ToArray();
            var i = 0;
            foreach (var cInfo in _repoContentToInstall)
                await InstallRepoC(cInfo.Value, cInfo.Key, contentProgress[i++]).ConfigureAwait(false);
            _installedContent.AddRange(_repoContentToInstall.Values);
        }

        private async Task InstallRepoC(SpecificVersion dep, IPackagedContent c, ProgressLeaf progress) {
            var repo = _repositories.First(x => x.HasMod(dep.Name));
            var modInfo = repo.GetMod(dep.Name);
            await
                repo.GetMod(dep.Name, _action.Paths.Path, _packPath, _pm.StatusRepo, _action.Force)
                    .ConfigureAwait(false);
            _installed.Add(Tuple.Create(c, modInfo.Value.GetVersionInfo()));
            progress.Finish();
            // TODO: Incremental info update, however this is hard due to implementation of SixSync atm..
        }

        async Task InstallPackages() {
            if (_packagesToInstall.Count == 0)
                return;

            await _pm.Repo.ClearObjectsAsync().ConfigureAwait(false);

            _pm.Progress = _packageProgress;
            var installedPackages =
                await
                    _pm.ProcessPackages(_packagesToInstall.Values, skipWhenFileMatches: !_action.Force)
                        .ConfigureAwait(false);

            var installedContent = installedPackages.Select(
                x =>
                    Tuple.Create(
                        _allInstallableContent.Select(c => c.Key).OfType<IPackagedContent>()
                            .FirstOrDefault(
                                c => c.PackageName.Equals(x.MetaData.Name, StringComparison.CurrentCultureIgnoreCase)),
                        x.MetaData.GetVersionInfo()))
                .Where(x => x.Item1 != null); // Null check because we might download additional packages defined in package dependencies :S
            _installed.AddRange(installedContent);
            _installedContent.AddRange(_packageContent.Values);
        }

        private Dictionary<IPackagedContent, SpecificVersion> OnlyWhenNewOrUpdateAvailable() => _packageContent.Where(x => {
                var syncInfo = GetInstalledInfo(x);
                // syncINfo = null: new download, VersionData not equal: new update
                return syncInfo == null || !syncInfo.VersionData.Equals(x.Value.VersionData);
            }).ToDictionary(x => x.Key, x => x.Value);

        private Dictionary<IPackagedContent, SpecificVersion> GetRepoContentToInstallOrUpdate() => _repoContent.Where(c => {
            var repo = _repositories.First(x => x.HasMod(c.Value.Name));
            return !repo.ExistsAndIsRightVersion(c.Value.Name, _action.Paths.Path);
        }).ToDictionary(x => x.Key, x => x.Value);

        private Dictionary<IPackagedContent, SpecificVersion> GetGroupContentToInstallOrUpdate()
            => _groupContent.Where(c => {
                var group = _groups.First(x => x.HasMod(c.Value.Name));
                return !group.ExistsAndIsRightVersion(c.Value.Name, _action.Paths.Path);
            }).ToDictionary(x => x.Key, x => x.Value);

        private void HandleStats() {
            var localPackageIndex = _pm.GetPackagesAsVersions();
            foreach (var p in _packagesToInstall) {
                if (localPackageIndex.ContainsKey(p.Value.Name)) {
                    if (localPackageIndex[p.Value.Name].Contains(p.Value.VersionInfo)) {
                        // already have version
                    } else {
                        HandleUpdateStats(p.Key);
                        // is Update
                    }
                } else {
                    HandleInstallStats(p.Key);
                    // is Install
                }
            }
        }

        SpecificVersion GetInstalledInfo(KeyValuePair<IPackagedContent, SpecificVersion> i) {
            switch (_action.CheckoutType) {
            case CheckoutType.NormalCheckout:
                return Package.ReadSynqInfoFile(_pm.WorkDir.GetChildDirectoryWithName(i.Value.Name));
            case CheckoutType.CheckoutWithoutRemoval: {
                // TODO: Cache per GlobalWorkingPath ??
                return Package.GetInstalledPackages(_pm.WorkDir).FirstOrDefault(x => x.Name.Equals(i.Value.Name));
            }
            }
            throw new NotSupportedException("Unknown " + _action.CheckoutType);
        }

        void HandleInstallStats(IPackagedContent key) {
            if (key is ModNetworkContent)
                _action.Status.Mods.Install.Add(key.Id);
            else if (key is MissionNetworkContent)
                _action.Status.Missions.Install.Add(key.Id);
            // TODO!
            else if (key is NetworkCollection)
                _action.Status.Collections.Install.Add(key.Id);
        }

        void HandleUpdateStats(IPackagedContent key) {
            if (key is ModNetworkContent)
                _action.Status.Mods.Update.Add(key.Id);
            else if (key is MissionNetworkContent)
                _action.Status.Missions.Update.Add(key.Id);
            // TODO!
            else if (key is NetworkCollection)
                _action.Status.Collections.Update.Add(key.Id);
        }

        async Task HandleRepositories() {
            _repositories =
                _action.Content.Select(x => x.Content).OfType<IHaveRepositories>()
                    .SelectMany(x => x.Repositories.Select(r => new CustomRepo(CustomRepo.GetRepoUri(new Uri(r)))))
                    .ToArray();
            foreach (var r in _repositories) {
                await
                    new AuthDownloadWrapper(_authProvider).WrapAction(
                        uri => r.Load(SyncEvilGlobal.StringDownloader, uri),
                        r.Uri).ConfigureAwait(false);
            }
        }

        private async Task HandleGroups() {
            _groups =
                _action.Content.Select(x => x.Content)
                    .OfType<IHaveGroup>()
                    .Where(x => x.GroupId.HasValue)
                    .Select(x => new Group(x.GroupId.Value, "Unknown"))
                    .ToArray();
            var token = await _authProvider.GetToken().ConfigureAwait(false);
            foreach (var g in _groups)
                await g.Load(token).ConfigureAwait(false);
        }

        void SetupPackageManager() {
            _pm = new PackageManager(_repository, _action.Paths.Path, true) {StatusRepo = _statusRepo};
            Sync.Core.Packages.CheckoutType ct;
            if (!Enum.TryParse(_action.CheckoutType.ToString(), out ct))
                throw new InvalidOperationException("Unsupported checkout type");
            _pm.Settings.CheckoutType = ct;
            _pm.Settings.GlobalWorkingPath = _action.GlobalWorkingPath;
        }

        IEnumerable<KeyValuePair<Guid, Uri[]>> GetRemotes(RemoteInfoAttribute synqRemoteInfo, bool isPremium)
            => isPremium ? synqRemoteInfo.PremiumRemotes : synqRemoteInfo.DefaultRemotes;

        Task InstallContent(IContentSpec<IInstallableContent> c) => c.Content.Install(this, _action.CancelToken, c.Constraint);

        class SixSyncProgress
        {
            public SixSyncProgress(ProgressComponent main, ProgressComponent processing) {
                Main = main;
                Processing = processing;
            }

            public ProgressComponent Main { get; }
            public ProgressComponent Processing { get; }
        }

        class RepoWatcher : IDisposable
        {
            const int TimerTime = 500;
            bool _disposed;
            TimerWithElapsedCancellation _timer;

            public RepoWatcher(StatusRepo repo) {
                _timer = new TimerWithElapsedCancellation(TimerTime, () => {
                    repo.UpdateTotals();
                    return true;
                });
            }

            public void Dispose() {
                Dispose(true);
            }

            protected virtual void Dispose(bool disposing) {
                if (!disposing)
                    return;

                if (_disposed)
                    return;
                _timer.Dispose();
                _timer = null;

                _disposed = true;
            }
        }

        // A cheap variant of ReactiveUI's this.WhenAny...
        // TODO: Choose if we will make all Core Domain free of PropertyChanged / RXUI (and use custom events etc)
        // or if we will give in ;-)
        class StatusRepoMonitor : IDisposable
        {
            readonly Action<double, long?> _progressCallback;
            readonly StatusRepo _repo;

            internal StatusRepoMonitor(StatusRepo repo, Action<double, long?> progressCallback) {
                _repo = repo;
                _progressCallback = progressCallback;
                _repo.PropertyChanged += RepoOnPropertyChanged;
            }

            internal StatusRepoMonitor(StatusRepo repo, Func<double, long?, Task> progressCallback) {
                _repo = repo;
                _progressCallback = (p, s) => progressCallback(p, s).Wait(); // pff
                _repo.PropertyChanged += RepoOnPropertyChanged;
            }

            public void Dispose() {
                _repo.PropertyChanged -= RepoOnPropertyChanged;
            }

            void RepoOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs) {
                if (propertyChangedEventArgs.PropertyName == "Info")
                    _progressCallback(_repo.Info.Progress, _repo.Info.Speed);
            }
        }
    }
}