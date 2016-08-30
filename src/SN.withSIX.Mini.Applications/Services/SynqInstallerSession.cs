// <copyright company="SIX Networks GmbH" file="SynqInstallerSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MoreLinq;
using NDepend.Path;
using SN.withSIX.ContentEngine.Core;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Core.Extensions;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Steam.Api.Services;
using SN.withSIX.Sync.Core;
using SN.withSIX.Sync.Core.Legacy.SixSync.CustomRepo;
using SN.withSIX.Sync.Core.Legacy.Status;
using SN.withSIX.Sync.Core.Packages;
using SN.withSIX.Sync.Core.Repositories;
using SN.withSIX.Sync.Core.Transfer;
using withSIX.Api.Models;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Extensions;
using CheckoutType = SN.withSIX.Mini.Core.Games.CheckoutType;
using Group = SN.withSIX.Mini.Core.Social.Group;

namespace SN.withSIX.Mini.Applications.Services
{
    public class SynqInstallerSession : IInstallerSession
    {
        readonly IInstallContentAction<IInstallableContent> _action;
        private readonly IAuthProvider _authProvider;
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

        private AverageContainer2 _averageSpeed;
        private Dictionary<IPackagedContent, SpecificVersion> _groupContent =
            new Dictionary<IPackagedContent, SpecificVersion>();
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
        private Dictionary<IPackagedContent, SpecificVersion> _repoContent =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _repoContentToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private IAbsoluteDirectoryPath _repoPath;
        private SixSyncProgress _repoProgress;
        IReadOnlyCollection<CustomRepo> _repositories = new List<CustomRepo>();
        Repository _repository;
        private Dictionary<IPackagedContent, SpecificVersion> _steamContent =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _steamContentToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _externalContent =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _externalContentToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();

        private ProgressComponent _steamProcessing;
        private ProgressComponent _steamProgress;
        private ProgressComponent _externalProcessing;
        private ProgressComponent _externalProgress;
        private IExternalFileDownloader _dl;

        public SynqInstallerSession(IInstallContentAction<IInstallableContent> action, IToolsCheat toolsInstaller,
            Func<bool> isPremium, Func<ProgressInfo, Task> statusChange, IContentEngine contentEngine,
            IAuthProvider authProvider, IExternalFileDownloader dl) {
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
            _dl = dl;
            _progress = new ProgressComponent("Stage");
            _averageSpeed = new AverageContainer2(20);
            //_progress.AddComponents(_preparingProgress = new ProgressLeaf("Preparing"));
        }

        public async Task Install(IReadOnlyCollection<IContentSpec<IPackagedContent>> content) {
            PrepareContent(content);
            _allContentToInstall.ForEach(x => StartProcessState(x.Key, x.Value.VersionData));
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
                await PublishIndividualItemStates().ConfigureAwait(false);
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
            using (Observable.Interval(TimeSpan.FromMilliseconds(500))
                    .ConcatTask(TryStatusChange)
                    .Subscribe()) {
                await PrepareGroupsAndRepositories().ConfigureAwait(false);
                // TODO: make it unneeded to initialize Synq stuff unless we actually need it..
                if (_action.RemoteInfo != null) {
                    using (_repository = new Repository(_repoPath, true)) {
                        SetupPackageManager();
                        await UpdateSynqRemotes().ConfigureAwait(false);
                        await InstallContent().ConfigureAwait(false);
                    }
                } else
                    await InstallContent().ConfigureAwait(false);
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
            _installableContent = content.ToDictionary(x => x.Content,
                x => new SpecificVersion(x.Content.GetFQN(x.Constraint)))
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
            PrepareExternalContent();
            PreparePackageContent();
            _allContentToInstall =
                _packagesToInstall.Concat(_repoContentToInstall)
                    .Concat(_groupContentToInstall)
                    .Concat(_steamContentToInstall)
                    .ToDictionary(x => x.Key, x => x.Value);
            //_preparingProgress.Finish();
        }

        private void PrepareExternalContent() {
            _externalContent = GetExternalContent();
            _externalContentToInstall = _externalContent; // TODO
            if (_externalContentToInstall.Any()) {
                _externalProgress = new ProgressComponent("External mods");
                _externalProcessing = new ProgressComponent("Processing");
                _externalProcessing.AddComponents(
                    _externalContentToInstall.Select(x => new ProgressLeaf(x.Key.Name)).ToArray());
                _externalProgress.AddComponents(_externalProcessing);
                _progress.AddComponents(_externalProgress);
            }
        }

        private Dictionary<IPackagedContent, SpecificVersion> GetExternalContent() => _installableContent
            .Except(_groupContent)
            .Except(_repoContent)
            .Select(x => new {Content = x.Key as INetworkContent, x.Value})
            .Where(x => ShouldInstallFromExternal(x.Content))
            .ToDictionary(x => x.Content as IPackagedContent, y => y.Value);

        private static bool ShouldInstallFromExternal(IContentWithPackageName content) => content.Source.Publisher.ShouldInstallFromExternal();

        private void PrepareSteamContent() {
            _steamContent = _action.Game.IsSteamGame()
                ? GetSteamContent()
                : new Dictionary<IPackagedContent, SpecificVersion>();
            _steamContentToInstall = _steamContent; // TODO
            if (_steamContentToInstall.Any()) {
                _steamProgress = new ProgressComponent("Steam mods");
                _steamProcessing = new ProgressComponent("Processing");
                _steamProcessing.AddComponents(
                    _steamContentToInstall.Select(x => new ProgressLeaf(x.Key.Name)).ToArray());
                _steamProgress.AddComponents(_steamProcessing);
                _progress.AddComponents(_steamProgress);
            }
        }

        private Dictionary<IPackagedContent, SpecificVersion> GetSteamContent() => _installableContent
            .Except(_groupContent)
            .Except(_repoContent)
            .Select(x => new {Content = x.Key as INetworkContent, x.Value})
            .Where(x => ShouldInstallFromSteam(x.Content))
            .ToDictionary(x => x.Content as IPackagedContent, y => y.Value);

        private static bool ShouldInstallFromSteam(INetworkContent content)
            => content.Source.Publisher == Publisher.Steam;

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
            _packageContent = GetPackagedContent();
            if (_action.RemoteInfo == null) {
                if (_packageContent.Any())
                    throw new NotSupportedException("This game currently does not support SynqPackages");
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

        private Dictionary<IPackagedContent, SpecificVersion> GetPackagedContent() => _installableContent
            .Except(_groupContent)
            .Except(_repoContent)
            .Except(_steamContent)
            .Except(_externalContent)
            .ToDictionary(x => x.Key, x => x.Value);

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
                    new ContentStatusChanged(c.Key, c.Key.ProcessingState, progress, speed)
                        .Raise()
                        .ConfigureAwait(false);
            }
        }

        private ItemState StartProcessState(IContent content, string version) {
            content.StartProcessingState(version, _action.Force);
            return content.ProcessingState;
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
            await InstallExternalContent().ConfigureAwait(false);
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
                cInfo.Key.FinishProcessingState(cInfo.Value, false);
            _action.Game.RefreshCollections();
        }

        private void MarkContentAsFinished() {
            foreach (var cInfo in _allInstallableContent)
                cInfo.Key.FinishProcessingState(cInfo.Value, true);
            _action.Game.RefreshCollections();
        }

        async Task PerformPostInstallTasks(Dictionary<IPackagedContent, SpecificVersion> installableContent) {
            foreach (var c in installableContent) {
                await
                    c.Key.PostInstall(this, _action.CancelToken, _installedContent.Contains(c.Value))
                        .ConfigureAwait(false);
            }
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
            foreach (var c in contentToInstall.Where(x => x.Key.GetState(x.Value.VersionData) == ItemState.NotInstalled)
                )
                HandleInstallStats(c.Key);
            foreach (
                var c in contentToInstall.Where(x => x.Key.GetState(x.Value.VersionData) == ItemState.UpdateAvailable))
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
            await PerformInstallSteamContent().ConfigureAwait(false);
            HandleInstallUpdateStats(_steamContentToInstall);
        }

        async Task InstallExternalContent() {
            if (_externalContentToInstall.Count == 0)
                return;
            await PerformInstallExternalContent().ConfigureAwait(false);
            HandleInstallUpdateStats(_externalContentToInstall);
        }

        private async Task PerformInstallSteamContent() {
            var contentProgress = _steamProcessing
                .GetComponents()
                .OfType<ProgressLeaf>()
                .ToArray();
            var i = 0;
            var session =
                new SteamExternalInstallerSession(
                    _action.Game.SteamInfo.AppId,
                    _action.Game.SteamworkshopPaths.ContentPath,
                    // TODO: Specific Steam path retrieved from Steam info, and separate the custom content location
                    _steamContentToInstall.ToDictionary(x => Convert.ToUInt64(x.Key.Source.PublisherId),
                        x => contentProgress[i++]));
            await session.Install(_action.CancelToken, _action.Force).ConfigureAwait(false);
            _installedContent.AddRange(_steamContentToInstall.Values);
        }

        private async Task PerformInstallExternalContent() {
            var contentProgress = _externalProcessing
                .GetComponents()
                .OfType<ProgressLeaf>()
                .ToArray();
            var i = 0;
            var session =
                new ExternalContentInstallerSession(_action.Paths.Path,
                    _externalContentToInstall.ToDictionary(x => x.Key.Source, x => contentProgress[i++]), _action.Game,
                    _dl);
            await session.Install(_action.CancelToken, _action.Force).ConfigureAwait(false);
            _installedContent.AddRange(_externalContentToInstall.Values);
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
                .Where(x => x.Item1 != null);
            // Null check because we might download additional packages defined in package dependencies :S
            _installed.AddRange(installedContent);
            _installedContent.AddRange(_packageContent.Values);
        }

        private Dictionary<IPackagedContent, SpecificVersion> OnlyWhenNewOrUpdateAvailable()
            => _packageContent.Where(x => {
                var syncInfo = GetInstalledInfo(x);
                // syncINfo = null: new download, VersionData not equal: new update
                return syncInfo == null || !syncInfo.VersionData.Equals(x.Value.VersionData);
            }).ToDictionary(x => x.Key, x => x.Value);

        private Dictionary<IPackagedContent, SpecificVersion> GetRepoContentToInstallOrUpdate()
            => _repoContent.Where(c => {
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

        Task InstallContent(IContentSpec<IInstallableContent> c)
            => c.Content.Install(this, _action.CancelToken, c.Constraint);

        internal class SteamExternalInstallerSession
        {
            private readonly uint _appId;
            private readonly Dictionary<ulong, ProgressLeaf> _content;
            private readonly SteamHelperParser _steamHelperParser;
            private readonly IAbsoluteDirectoryPath _workshopPath;

            public SteamExternalInstallerSession(uint appId, IAbsoluteDirectoryPath workshopPath,
                Dictionary<ulong, ProgressLeaf> content) {
                _appId = appId;
                _workshopPath = workshopPath;
                _content = content;
                _steamHelperParser = new SteamHelperParser(content);
            }

            public Task Install(CancellationToken cancelToken, bool force) {
                var options = new List<string>();
                if (force)
                    options.Add("--force");
                return RunHelper(cancelToken, "install", options.ToArray());
            }

            public Task Uninstall(CancellationToken cancelToken) {
                var options = new List<string>();
                return RunHelper(cancelToken, "uninstall", options.ToArray());
            }

            private async Task RunHelper(CancellationToken cancelToken, string cmd, params string[] options) {
                var helperExe = GetHelperExecutable();
                var r =
                    await
                        Tools.ProcessManager.LaunchAndProcessAsync(
                            new LaunchAndProcessInfo(new ProcessStartInfo(helperExe.ToString(),
                                GetHelperParameters(cmd, options).CombineParameters()) {
                                    WorkingDirectory = helperExe.ParentDirectoryPath.ToString()
                                }) {
                                    StandardOutputAction = _steamHelperParser.ProcessProgress,
                                    StandardErrorAction =
                                        (process, s) => MainLog.Logger.Warn("SteamHelper ErrorOut: " + s),
                                    CancellationToken = cancelToken
                                }).ConfigureAwait(false);
                ProcessExitResult(r);
            }

            private static void ProcessExitResult(ProcessExitResult r) {
                switch (r.ExitCode) {
                case 3:
                    throw new SteamInitializationException(
                        "The Steam client does not appear to be running, or runs under different (Administrator?) priviledges. Please start Steam and/or restart the withSIX client under the same priviledges");
                case 4:
                    throw new SteamNotFoundException(
                        "The Steam client does not appear to be running, nor was Steam found");
                case 9:
                    throw new TimeoutException("The operation timed out waiting for a response from the Steam client");
                case 10:
                    throw new OperationCanceledException("The operation was canceled");
                }
                r.ConfirmSuccess();
            }

            private static IAbsoluteFilePath GetHelperExecutable() => Common.Paths.AppPath
                .GetChildFileWithName("SteamHelper.exe");

            private IEnumerable<string> GetHelperParameters(string command, params string[] options) {
                if (Common.Flags.Verbose)
                    options = options.Concat(new[] {"--verbose"}).ToArray();
                return new[] {command, "-a", _appId.ToString()}.Concat(options).Concat(_content.Keys.Select(Selector));
            }

            private string Selector(ulong x) {
                var xStr = x.ToString();
                return !_workshopPath.GetChildDirectoryWithName(xStr).Exists ? $"!{xStr}" : xStr;
            }

            private class SteamHelperParser
            {
                private static readonly Regex rxStart = new Regex(@"Starting (\d+)");
                private static readonly Regex rxEnd = new Regex(@"Finished (\d+)");
                private static readonly Regex rxProgress = new Regex(@"(\d*)/s (\d+([\.,]\d+)?)%");
                private readonly Dictionary<ulong, ProgressLeaf> _content;
                private ProgressLeaf _current;

                public SteamHelperParser(Dictionary<ulong, ProgressLeaf> content) {
                    _content = content;
                }

                public void ProcessProgress(Process _, string x) {
                    var progressMatch = rxProgress.Match(x);
                    if (progressMatch.Success) {
                        var value = progressMatch.Groups[1].Value;
                        var speed = string.IsNullOrWhiteSpace(value) ? null : (long?) Convert.ToInt64(value);
                        value = progressMatch.Groups[2].Value;
                        var progress = string.IsNullOrWhiteSpace(value) ? 0.0 : Convert.ToDouble(value);
                        _current.Update(speed, progress);
                    } else {
                        var startMatch = rxStart.Match(x);
                        if (startMatch.Success)
                            _current = _content[Convert.ToUInt64(startMatch.Groups[1].Value)];
                        else {
                            var endMatch = rxEnd.Match(x);
                            if (endMatch.Success)
                                _current.Finish();
                        }
                    }
                }
            }
        }

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

    internal class ExternalContentInstallerSession
    {
        private readonly IAbsoluteDirectoryPath _contentPath;
        private readonly Dictionary<ContentPublisher, ProgressLeaf> _content;
        private readonly Game _game;
        private readonly IExternalFileDownloader _dl;

        public ExternalContentInstallerSession(IAbsoluteDirectoryPath contentPath, Dictionary<ContentPublisher, ProgressLeaf> content, Game game, IExternalFileDownloader dl) {
            _contentPath = contentPath;
            _content = content;
            _game = game;
            _dl = dl;
        }

        public async Task Install(CancellationToken cancelToken, bool force) {
            // Contact Node, tell it to download from URL, to Directory
            // TODO: Progress reporting
            foreach (var c in _content) {
                var f = await _dl.DownloadFile(_game.GetPublisherUrl(c.Key), _contentPath, c.Value.Update).ConfigureAwait(false);
                var destinationDir = _contentPath.GetChildDirectoryWithName(c.Key.PublisherId);
                if (destinationDir.Exists)
                    destinationDir.Delete(true);
                if (f.IsArchive())
                    f.Unpack(destinationDir, true);
                else {
                    destinationDir.Create();
                    f.Move(destinationDir);
                }
            }
        }
    }

    public interface IExternalFileDownloader
    {
        Task<IAbsoluteFilePath> DownloadFile(Uri url, IAbsoluteDirectoryPath destination,
            Action<long?, double> progressAction, CancellationToken cancelToken = default(CancellationToken));
        bool RegisterExisting(Uri url, IAbsoluteFilePath path);
    }
}