// <copyright company="SIX Networks GmbH" file="InstallerSession.cs">
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
using ReactiveUI;
using SN.withSIX.ContentEngine.Core;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Models;
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
    public class InstallerSession : IInstallerSession
    {
        readonly IInstallContentAction<IInstallableContent> _action;

        private readonly Dictionary<IContent, SpecificVersion> _completed = new Dictionary<IContent, SpecificVersion>();
        readonly IContentEngine _contentEngine;
        private readonly IExternalFileDownloader _dl;
        private readonly Dictionary<IContent, SpecificVersion> _failed = new Dictionary<IContent, SpecificVersion>();

        readonly List<Exception> _notInstallable = new List<Exception>();
        private readonly PackageInstaller _packageInstaller;
        //private readonly ProgressLeaf _preparingProgress;
        private readonly ProgressComponent _progress;
        private readonly SixSyncInstaller _sixSyncInstaller;
        readonly Func<ProgressInfo, Task> _statusChange;
        readonly IToolsCheat _toolsInstaller;
        private IDictionary<IPackagedContent, SpecificVersion> _allContentToInstall;
        private Dictionary<IContent, SpecificVersion> _allInstallableContent =
            new Dictionary<IContent, SpecificVersion>();

        private AverageContainer2 _averageSpeed;
        private Dictionary<IPackagedContent, SpecificVersion> _externalContent =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _externalContentToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private ProgressComponent _externalProcessing;
        private ProgressComponent _externalProgress;
        private Dictionary<IPackagedContent, SpecificVersion> _groupContent =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _groupContentToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private SixSyncProgress _groupProgress;
        private Dictionary<IPackagedContent, SpecificVersion> _installableContent;
        private Dictionary<IPackagedContent, SpecificVersion> _packageContent =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private PackageProgress _packageProgress;
        private Dictionary<IPackagedContent, SpecificVersion> _packagesToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private IAbsoluteDirectoryPath _packPath;
        private IDictionary<IContent, SpecificVersion> _postInstallCompleted = new Dictionary<IContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _repoContent =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _repoContentToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private IAbsoluteDirectoryPath _repoPath;
        private SixSyncProgress _repoProgress;

        private Dictionary<IPackagedContent, SpecificVersion> _steamContent =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private Dictionary<IPackagedContent, SpecificVersion> _steamContentToInstall =
            new Dictionary<IPackagedContent, SpecificVersion>();
        private ProgressComponent _steamProcessing;
        private ProgressComponent _steamProgress;

        public InstallerSession(IInstallContentAction<IInstallableContent> action, IToolsCheat toolsInstaller,
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
            _statusChange = statusChange;
            _contentEngine = contentEngine;
            _dl = dl;
            _progress = new ProgressComponent("Stage");
            _averageSpeed = new AverageContainer2(20);
            _packageInstaller = new PackageInstaller(_action, isPremium);
            _sixSyncInstaller = new SixSyncInstaller(_action, TryLegacyStatusChange, authProvider);
            //_progress.AddComponents(_preparingProgress = new ProgressLeaf("Preparing"));
        }

        public async Task Install(IReadOnlyCollection<IContentSpec<IPackagedContent>> content) {
            PrepareContent(content);
            await ConfirmExternalContent().ConfigureAwait(false);
            _allContentToInstall.ForEach(x => StartProcessState(x.Key, x.Value.VersionData));
            await PublishIndividualItemStates().ConfigureAwait(false);

            try {
                await TryInstallContent().ConfigureAwait(false);
            } finally {
                await ProcessStates().ConfigureAwait(false);
                if (_notInstallable.Any()) {
                    await
                        UserError.Throw(new UserError("Some content is not installable",
                            string.Join(", ", _notInstallable.Select(x => x.Message)),
                            new[] {RecoveryCommandImmediate.Ok}));
                }
            }
        }

        public async Task Synchronize() {
            // TODO: Install Tools as part of the Setup process instead? Also include in Setup package..
            await InstallToolsIfNeeded().ConfigureAwait(false);
            //_preparingProgress.Progress = 10;
            CreateDirectories();
            await PerformInstallation().ConfigureAwait(false);
        }

        public void RunCE(IPackagedContent content) {
            if (_contentEngine.ModHasScript(content.Id)) {
                _contentEngine.LoadModS(new ContentEngineContent(content.Id, content.Id, true,
                    _action.Paths.Path.GetChildDirectoryWithName(content.PackageName),
                    content.GameId)).processMod();
            }
        }

        private async Task ProcessStates() {
            HandleInstallUpdateStats();
            MarkContentStates();
            await PublishIndividualItemStates().ConfigureAwait(false);
        }

        private async Task PerformInstallation() {
            try {
                using (Observable.Interval(TimeSpan.FromMilliseconds(500))
                    .ConcatTask(TryStatusChange)
                    .Subscribe()) {
                    await _sixSyncInstaller.PrepareGroupsAndRepositories().ConfigureAwait(false);
                    await InstallContent().ConfigureAwait(false);
                }
            } catch (AggregateException ex) {
                throw GetFirstNotExternalDownloadCancelled(ex) ?? new OperationCanceledException();
            }
        }

        private static Exception GetFirstNotExternalDownloadCancelled(AggregateException ex) {
            var errors = ex.Flatten().InnerExceptions;
            return errors.NotOfType<Exception, ExternalDownloadCancelled>().FirstOrDefault();
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
            using (this.Bench("Creating directories and setting ACLs"))
                CreateDirectoriesInternal();
        }

        private void CreateDirectoriesInternal() {
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
            BuildInstallableContent(content);

            // Add collections if available
            BuildAllInstallableContent();
            PrepareGroupContent();
            PrepareRepoContent();
            PrepareSteamContent();
            PrepareExternalContent();
            PreparePackageContent();
            BuildAllContentToInstall();
            //_preparingProgress.Finish();
        }

        private void BuildInstallableContent(IEnumerable<IContentSpec<IPackagedContent>> content)
            => _installableContent = content.ToDictionary(x => x.Content,
                x => new SpecificVersion(x.Content.GetFQN(x.Constraint)))
                .Where(x => !_completed.Values.Contains(x.Value))
                .ToDictionary(x => x.Key, x => x.Value);

        private void BuildAllInstallableContent() => _allInstallableContent =
            _installableContent.ToDictionary(x => (IContent) x.Key, x => x.Value)
                .Concat(_action.Content.ToDictionary(x => (IContent) x.Content,
                    x => new SpecificVersion(x.Constraint ?? x.Content.Version)))
                .DistinctBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);

        private void BuildAllContentToInstall() => _allContentToInstall =
            _packagesToInstall
                .Concat(_repoContentToInstall)
                .Concat(_groupContentToInstall)
                .Concat(_steamContentToInstall)
                .Concat(_externalContentToInstall)
                .ToDictionary(x => x.Key, x => x.Value);

        private void MarkPrepared(Dictionary<IPackagedContent, SpecificVersion> specificVersions)
            => _installableContent = _installableContent.Except(specificVersions).ToDictionary(x => x.Key, x => x.Value);

        private void PrepareExternalContent() {
            MarkPrepared(_externalContent = GetExternalContent());
            _externalContentToInstall = ToProcessContent(_externalContent);
            if (!_externalContentToInstall.Any())
                return;
            _externalProgress = new ProgressComponent("External mods");
            _externalProcessing = new ProgressComponent("Processing");
            _externalProcessing.AddComponents(
                _externalContentToInstall.Select(x => new ProgressLeaf(x.Key.PackageName)).ToArray());
            _externalProgress.AddComponents(_externalProcessing);
            _progress.AddComponents(_externalProgress);
        }

        private Dictionary<IPackagedContent, SpecificVersion> ToProcessContent(
            Dictionary<IPackagedContent, SpecificVersion> content) => _action.Force
                ? content
                : OnlyWhenNewOrUpdateAvailable(content).ToDictionary(x => x.Key, x => x.Value);

        private Dictionary<IPackagedContent, SpecificVersion> GetExternalContent() => _installableContent
            .Where(x => ShouldInstallFromExternal(x.Key))
            .ToDictionary(x => x.Key, y => y.Value);

        private bool ShouldInstallFromExternal(IContentWithPackageName content)
            => content.GetSource(_action.Game).Publisher.ShouldInstallFromExternal();

        private void PrepareSteamContent() {
            MarkPrepared(_steamContent = _action.Game.IsSteamGame()
                ? GetSteamContent()
                : new Dictionary<IPackagedContent, SpecificVersion>());
            _steamContentToInstall = ToProcessContent(_steamContent);
            if (!_steamContentToInstall.Any())
                return;
            _steamProgress = new ProgressComponent("Steam mods");
            _steamProcessing = new ProgressComponent("Processing");
            _steamProcessing.AddComponents(
                _steamContentToInstall.Select(x => new ProgressLeaf(x.Key.PackageName)).ToArray());
            _steamProgress.AddComponents(_steamProcessing);
            _progress.AddComponents(_steamProgress);
        }

        private Dictionary<IPackagedContent, SpecificVersion> GetSteamContent() => _installableContent
            .Select(x => new {Content = x.Key as INetworkContent, x.Value})
            .Where(x => ShouldInstallFromSteam(x.Content))
            .ToDictionary(x => x.Content as IPackagedContent, y => y.Value);

        private bool ShouldInstallFromSteam(IContentWithPackageName content)
            => content.GetSource(_action.Game).Publisher == Publisher.Steam;

        private void PrepareGroupContent() {
            MarkPrepared(_groupContent = _installableContent
                .Where(x => _sixSyncInstaller.Groups.Any(r => r.HasMod(x.Value.Name)))
                .ToDictionary(x => x.Key, x => x.Value));
            _groupContentToInstall = _action.Force ? _groupContent : GetGroupContentToInstallOrUpdate();
            _groupProgress = SetupSixSyncProgress("Group mods", _groupContentToInstall);
        }

        private void PrepareRepoContent() {
            MarkPrepared(_repoContent = _installableContent
                .Where(x => _sixSyncInstaller.Repositories.Any(r => r.HasMod(x.Value.Name)))
                .ToDictionary(x => x.Key, x => x.Value));
            _repoContentToInstall = _action.Force ? _repoContent : GetRepoContentToInstallOrUpdate();
            _repoProgress = SetupSixSyncProgress("Repo mods", _repoContentToInstall);
        }

        private void PreparePackageContent() {
            MarkPrepared(_packageContent = _installableContent);
            if (_action.RemoteInfo == null) {
                if (_packageContent.Any())
                    throw new NotSupportedException("This game currently does not support SynqPackages");
                _packagesToInstall = _packageContent;
            } else {
                _packagesToInstall = GetPackageContentToInstall();
                _packageProgress = SetupSynqProgress("Network mods", _packagesToInstall);
            }
        }

        private Dictionary<IPackagedContent, SpecificVersion> GetPackageContentToInstall() => _action.Force
            ? _packageContent
            : OnlyWhenNewOrUpdateAvailable()
                .ToDictionary(x => x.Key, x => x.Value);

        private SixSyncProgress SetupSixSyncProgress(string title,
            Dictionary<IPackagedContent, SpecificVersion> progressContent) {
            if (!progressContent.Any())
                return null;

            var modComponents = new ProgressComponent(title, progressContent.Count);
            var processingComponent = new ProgressComponent("Downloading");
            processingComponent.AddComponents(progressContent.Select(x => new ProgressLeaf(x.Key.PackageName)).ToArray());
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

        private void StartProcessState(IContent content, string version)
            => content.StartProcessingState(version, _action.Force);

        // TODO: Rethink our strategy here. We convert it to a temp collection so that we can install all the content at once.
        // this is because we relinquish control to the content.Install method, and that will build the dependency tree and call the installer actions with it..
        Task InstallContent() => InstallContent(ConvertToTemporaryCollectionSpec());

        IContentSpec<IInstallableContent> ConvertToTemporaryCollectionSpec() => _action.Content.Count == 1
            ? _action.Content.First()
            : new InstallContentSpec(ConvertToTemporaryCollection());

        LocalCollection ConvertToTemporaryCollection()
            =>
                new LocalCollection(_action.Game.Id,
                    _action.Content.Select(x => new ContentSpec((Content) x.Content, x.Constraint)).ToList());

        Task TryInstallContent() => TaskExtExt.Create(
            InstallPackages,
            InstallSteamContent,
            InstallExternalContent,
            InstallGroupAndRepoContent,
            PerformPostInstallTasks
            ).RunAndThrow();

        private async Task InstallGroupAndRepoContent() {
            _averageSpeed = new AverageContainer2(20);
            try {
                await TaskExtExt.Create(InstallGroupContent, InstallRepoContent).RunAndThrow().ConfigureAwait(false);
            } finally {
                ProcessSessionResults(_sixSyncInstaller);
            }
        }

        private async Task ConfirmExternalContent() {
            if (!_externalContentToInstall.Any())
                return;
            var result = await ConfirmUser();
            if (result ==
                RecoveryOptionResult.CancelOperation)
                throw new OperationCanceledException("The user cancelled the operation");
        }

        private IObservable<RecoveryOptionResult> ConfirmUser()
            => UserError.Throw(new UserError("External Content found",
                "The following content will be downloaded from External websites,\nDuring the process, a window will open and you will need to click the respective Download buttons for the the following Content:\n" +
                string.Join(", ", _externalContentToInstall.Select(x => x.Key.PackageName)) +
                "\n\nPress OK to Continue",
                new[] {RecoveryCommandImmediate.Ok, RecoveryCommandImmediate.Cancel}));

        private void MarkContentStates() {
            // TODO: How about marking this content at the start, much like .Use() for RecentItems
            // then even if the user restarts the computer / terminates the app, the state is preserved.
            // TODO: Minus the _installed content... however, they are not fully installed anyway until their postinstall tasks have completed..
            var failedContent = _allInstallableContent.Except(_postInstallCompleted).ToArray();
            foreach (var cInfo in failedContent)
                cInfo.Key.FinishProcessingState(cInfo.Value.VersionData, false);

            // Include or not Include Collections // TODO: Collections also have special Installed case (call in Install method)
            foreach (var cInfo in failedContent.Any() ? _postInstallCompleted : _allInstallableContent)
                cInfo.Key.FinishProcessingState(cInfo.Value.VersionData, true);

            _action.Game.RefreshCollections();
        }

        async Task PerformPostInstallTasks() {
            var postInstaller = new PostInstaller(this);
            try {
                await
                    postInstaller.PostInstallHandler(_allInstallableContent.Except(_failed), _action.CancelToken,
                        _completed.Values)
                        .ConfigureAwait(false);
            } catch (AggregateException ex) {
                var flatten = ex.Flatten();
                if (flatten.InnerExceptions.All(x => x is NotInstallableException))
                    _notInstallable.AddRange(flatten.InnerExceptions);
                else
                    throw;
            } finally {
                _postInstallCompleted = postInstaller.Completed;
            }
        }

        Task InstallToolsIfNeeded() => _toolsInstaller.SingleToolsInstallTask(_action.CancelToken);

        async Task InstallGroupContent() {
            if (_groupContentToInstall.Count == 0)
                return;
            await
                _sixSyncInstaller.InstallGroupContent(_groupContentToInstall, _groupProgress, _packPath)
                    .ConfigureAwait(false);
        }

        private void HandleInstallUpdateStats() {
            foreach (var c in _completed.Where(x => x.Key.GetState(x.Value.VersionData) == ItemState.NotInstalled))
                HandleInstallStats(c.Key, _action.Status);
            foreach (var c in _completed.Where(x => x.Key.GetState(x.Value.VersionData) == ItemState.UpdateAvailable))
                HandleUpdateStats(c.Key, _action.Status);
        }

        protected static void HandleInstallStats(IContent key, InstallStatusOverview installStatusOverview) {
            if (key is ModNetworkContent)
                installStatusOverview.Mods.Install.Add(key.Id);
            else if (key is MissionNetworkContent)
                installStatusOverview.Missions.Install.Add(key.Id);
            // TODO!
            else if (key is NetworkCollection)
                installStatusOverview.Collections.Install.Add(key.Id);
        }

        protected static void HandleUpdateStats(IContent key, InstallStatusOverview installStatusOverview) {
            if (key is ModNetworkContent)
                installStatusOverview.Mods.Update.Add(key.Id);
            else if (key is MissionNetworkContent)
                installStatusOverview.Missions.Update.Add(key.Id);
            // TODO!
            else if (key is NetworkCollection)
                installStatusOverview.Collections.Update.Add(key.Id);
        }

        async Task InstallSteamContent() {
            if (_steamContentToInstall.Count == 0)
                return;
            _averageSpeed = new AverageContainer2(20);
            await PerformInstallSteamContent().ConfigureAwait(false);
        }

        async Task InstallExternalContent() {
            if (_externalContentToInstall.Count == 0)
                return;
            _averageSpeed = new AverageContainer2(20);
            await PerformInstallExternalContent().ConfigureAwait(false);
        }

        private async Task PerformInstallSteamContent() {
            var contentProgress = _steamProcessing
                .GetComponents()
                .OfType<ProgressLeaf>()
                .ToArray();
            var session = new SteamSession(_action, _steamContentToInstall, contentProgress);
            try {
                await session.Install().ConfigureAwait(false);
            } finally {
                ProcessSessionResults(session);
            }
        }

        private async Task PerformInstallExternalContent() {
            var contentProgress = _externalProcessing
                .GetComponents()
                .OfType<ProgressLeaf>()
                .ToArray();
            var i = 0;
            var session =
                new ExternalContentInstallerSession(_action.Paths.Path, _externalContentToInstall, contentProgress,
                    _action.Game,
                    _dl);
            try {
                await session.Install(_action.CancelToken, _action.Force).ConfigureAwait(false);
            } finally {
                ProcessSessionResults(session);
            }
        }

        private void ProcessSessionResults(Session session) {
            _completed.AddRange(ConvertPairs(session.Completed));
            _failed.AddRange(ConvertPairs(session.Failed));
        }

        private static IEnumerable<KeyValuePair<IContent, SpecificVersion>> ConvertPairs(
            IDictionary<IPackagedContent, SpecificVersion> r)
            => r.Select(x => new KeyValuePair<IContent, SpecificVersion>(x.Key, x.Value));

        async Task InstallRepoContent() {
            if (_repoContentToInstall.Count == 0)
                return;
            await
                _sixSyncInstaller.InstallRepoContent(_repoContentToInstall, _repoProgress, _packPath)
                    .ConfigureAwait(false);
        }

        async Task InstallPackages() {
            if (_packagesToInstall.Count == 0)
                return;

            try {
                await
                    _packageInstaller.Install(_packagesToInstall, _action.Force, _packageProgress, _repoPath)
                        .ConfigureAwait(false);
            } finally {
                ProcessSessionResults(_packageInstaller);
            }

            /*
            var installedContent = installedPackages.Select(
                x =>
                    Tuple.Create(
                        _allInstallableContent.Select(c => c.Key).OfType<IPackagedContent>()
                            .FirstOrDefault(
                                c => c.PackageName.Equals(x.MetaData.Name, StringComparison.CurrentCultureIgnoreCase)),
                        x.MetaData.GetVersionInfo()))
            // Null check because we might download additional packages defined in package dependencies :S
                .Where(x => x.Item1 != null);
            */
        }

        private IEnumerable<KeyValuePair<IPackagedContent, SpecificVersion>> OnlyWhenNewOrUpdateAvailable(
            IEnumerable<KeyValuePair<IPackagedContent, SpecificVersion>> dict)
            => dict.Where(x => x.Key.GetState(x.Value) != ItemState.Uptodate);
        
        private IEnumerable<KeyValuePair<IPackagedContent, SpecificVersion>> OnlyWhenNewOrUpdateAvailable()
            => _packageContent.Where(x => {
                var syncInfo = GetInstalledInfo(x);
                // syncINfo = null: new download, VersionData not equal: new update
                return syncInfo == null || !syncInfo.VersionData.Equals(x.Value.VersionData);
            });

        private Dictionary<IPackagedContent, SpecificVersion> GetRepoContentToInstallOrUpdate()
            => _repoContent.Where(c => {
                var repo = _sixSyncInstaller.Repositories.First(x => x.HasMod(c.Value.Name));
                return !repo.ExistsAndIsRightVersion(c.Value.Name, _action.Paths.Path);
            }).ToDictionary(x => x.Key, x => x.Value);

        private Dictionary<IPackagedContent, SpecificVersion> GetGroupContentToInstallOrUpdate()
            => _groupContent.Where(c => {
                var group = _sixSyncInstaller.Groups.First(x => x.HasMod(c.Value.Name));
                return !group.ExistsAndIsRightVersion(c.Value.Name, _action.Paths.Path);
            }).ToDictionary(x => x.Key, x => x.Value);

        SpecificVersion GetInstalledInfo(KeyValuePair<IPackagedContent, SpecificVersion> i) {
            switch (_action.CheckoutType) {
            case CheckoutType.NormalCheckout:
                return Package.ReadSynqInfoFile(_action.Paths.Path.GetChildDirectoryWithName(i.Value.Name));
            case CheckoutType.CheckoutWithoutRemoval: {
                // TODO: Cache per GlobalWorkingPath ??
                return Package.GetInstalledPackages(_action.Paths.Path).FirstOrDefault(x => x.Name.Equals(i.Value.Name));
            }
            }
            throw new NotSupportedException("Unknown " + _action.CheckoutType);
        }

        Task InstallContent(IContentSpec<IInstallableContent> c)
            => c.Content.Install(this, _action.CancelToken, c.Constraint);

        class SteamSession : Session
        {
            private readonly IInstallContentAction<IInstallableContent> _action;
            private readonly ProgressLeaf[] _contentProgress;
            private readonly IDictionary<IPackagedContent, SpecificVersion> _steamContentToInstall;

            public SteamSession(IInstallContentAction<IInstallableContent> action,
                IDictionary<IPackagedContent, SpecificVersion> steamContentToInstall, ProgressLeaf[] contentProgress) {
                _action = action;
                _steamContentToInstall = steamContentToInstall;
                _contentProgress = contentProgress;
            }

            public async Task Install() {
                var i = 0;
                var session =
                    new SteamExternalInstallerSession(
                        _action.Game.SteamInfo.AppId,
                        _action.Game.SteamDirectories.Workshop.ContentPath,
                        // TODO: Specific Steam path retrieved from Steam info, and separate the custom content location
                        _steamContentToInstall.ToDictionary(
                            x => Convert.ToUInt64(x.Key.GetSource(_action.Game).PublisherId),
                            x => _contentProgress[i++]));
                try {
                    await session.Install(_action.CancelToken, _action.Force).ConfigureAwait(false);
                } catch (Exception) {
                    Failed.AddRange(_steamContentToInstall);
                    throw;
                }
                Completed.AddRange(_steamContentToInstall);
            }
        }

        class PostInstaller : SessionBase<IContent, SpecificVersion>
        {
            private readonly IInstallerSession _installerSession;

            public PostInstaller(IInstallerSession installerSession) {
                _installerSession = installerSession;
            }

            public Task PostInstallHandler(IEnumerable<KeyValuePair<IContent, SpecificVersion>> toPostProcess,
                CancellationToken cancellationToken, IReadOnlyCollection<SpecificVersion> installedContent) =>
                    RunAndThrow(toPostProcess, async c => {
                        try {
                            await
                                c.Key.PostInstall(_installerSession, cancellationToken,
                                    installedContent.Contains(c.Value)).ConfigureAwait(false);
                        } catch (NotInstallableException) {
                            Completed.Add(c.Key, c.Value);
                            throw;
                        }
                    });
        }

        class SixSyncInstaller : Session
        {
            readonly IInstallContentAction<IInstallableContent> _action;
            private readonly IAuthProvider _authProvider;

            private readonly Func<double, long?, Task> _tryLegacyStatusChange;
            private StatusRepo _statusRepo;
            internal IReadOnlyCollection<Group> Groups = new List<Group>();
            internal IReadOnlyCollection<CustomRepo> Repositories = new List<CustomRepo>();

            public SixSyncInstaller(IInstallContentAction<IInstallableContent> action,
                Func<double, long?, Task> tryLegacyStatusChange, IAuthProvider authProvider) {
                _tryLegacyStatusChange = tryLegacyStatusChange;
                _action = action;
                _authProvider = authProvider;
                _statusRepo = new StatusRepo(_action.CancelToken);
            }

            public async Task PrepareGroupsAndRepositories() {
                //_preparingProgress.Progress = 50;
                await HandleGroups().ConfigureAwait(false);
                //_preparingProgress.Progress = 70;
                await HandleRepositories().ConfigureAwait(false);
            }

            public async Task InstallGroupContent(
                Dictionary<IPackagedContent, SpecificVersion> groupContentToInstall, SixSyncProgress progress,
                IAbsoluteDirectoryPath packPath) {
                var contentProgress =
                    progress.Processing
                        .GetComponents()
                        .OfType<ProgressLeaf>()
                        .ToArray();
                var i = 0;
                using (new RepoWatcher(_statusRepo))
                using (new StatusRepoMonitor(_statusRepo, _tryLegacyStatusChange)) {
                    await
                        RunAndThrow(groupContentToInstall,
                            x => InstallGroupC(x.Value, x.Key, contentProgress[i++], packPath))
                            .ConfigureAwait(false);
                }
            }

            private async Task InstallGroupC(SpecificVersion dep, IPackagedContent c, ProgressLeaf progressComponent,
                IAbsoluteDirectoryPath packPath) {
                var group = Groups.First(x => x.HasMod(dep.Name));
                var modInfo = @group.GetMod(dep.Name);
                await
                    @group.GetMod(modInfo, _action.Paths.Path, packPath,
                        _statusRepo, _authProvider, _action.Force)
                        .ConfigureAwait(false);
                progressComponent.Finish();
                // TODO: Incremental info update, however this is hard due to implementation of SixSync atm..
            }

            public async Task InstallRepoContent(Dictionary<IPackagedContent, SpecificVersion> repoContentToInstall,
                SixSyncProgress sixSyncProgress, IAbsoluteDirectoryPath packPath) {
                var contentProgress =
                    sixSyncProgress.Processing
                        .GetComponents()
                        .OfType<ProgressLeaf>()
                        .ToArray();
                var i = 0;
                _statusRepo = new StatusRepo(_action.CancelToken);
                using (new RepoWatcher(_statusRepo))
                using (new StatusRepoMonitor(_statusRepo, _tryLegacyStatusChange)) {
                    await
                        RunAndThrow(repoContentToInstall,
                            x => InstallRepoC(x.Value, x.Key, contentProgress[i++], packPath))
                            .ConfigureAwait(false);
                }
            }

            private async Task InstallRepoC(SpecificVersion dep, IPackagedContent c, ProgressLeaf progress,
                IAbsoluteDirectoryPath packPath) {
                var repo = Repositories.First(x => x.HasMod(dep.Name));
                var modInfo = repo.GetMod(dep.Name);
                await
                    repo.GetMod(dep.Name, _action.Paths.Path, packPath,
                        _statusRepo, _action.Force)
                        .ConfigureAwait(false);
                progress.Finish();
                // TODO: Incremental info update, however this is hard due to implementation of SixSync atm..
            }

            private async Task HandleRepositories() {
                Repositories = _action.Content.Select(x => x.Content).OfType<IHaveRepositories>()
                    .SelectMany(x => x.Repositories.Select(r => new CustomRepo(CustomRepo.GetRepoUri(new Uri(r)))))
                    .ToArray();
                foreach (var r in Repositories) {
                    await
                        new AuthDownloadWrapper(_authProvider).WrapAction(
                            uri => r.Load(SyncEvilGlobal.StringDownloader, uri),
                            r.Uri).ConfigureAwait(false);
                }
            }

            private async Task HandleGroups() {
                Groups = _action.Content.Select(x => x.Content)
                    .OfType<IHaveGroup>()
                    .Where(x => x.GroupId.HasValue)
                    .Select(x => new Group(x.GroupId.Value, "Unknown"))
                    .ToArray();
                var token = await _authProvider.GetToken().ConfigureAwait(false);
                foreach (var g in Groups)
                    await g.Load(token).ConfigureAwait(false);
            }
        }

        abstract class SessionBase<T, T2>
        {
            public IDictionary<T, T2> Completed { get; } =
                new Dictionary<T, T2>();
            public IDictionary<T, T2> Failed { get; } =
                new Dictionary<T, T2>();

            protected Task RunAndThrow(IEnumerable<KeyValuePair<T, T2>> c, Func<KeyValuePair<T, T2>, Task> act) =>
                c.Select(cInfo => new Func<Task>(async () => {
                    try {
                        await act(cInfo).ConfigureAwait(false);
                        Completed.Add(cInfo.Key, cInfo.Value);
                    } catch (OperationCanceledException) {
                        throw;
                    } catch (Exception) {
                        Failed.Add(cInfo.Key, cInfo.Value);
                        throw;
                    }
                })).RunAndThrow();
        }

        abstract class Session : SessionBase<IPackagedContent, SpecificVersion> {}

        class PackageInstaller : Session
        {
            private readonly IInstallContentAction<IInstallableContent> _action;
            private readonly Func<bool> _getIsPremium;
            private readonly StatusRepo _statusRepo;
            private PackageManager _pm;
            private bool _synqInitialized;

            public PackageInstaller(IInstallContentAction<IInstallableContent> action, Func<bool> getIsPremium) {
                _action = action;
                _getIsPremium = getIsPremium;
                _statusRepo = new StatusRepo(_action.CancelToken);
            }

            private async Task UpdateSynqRemotes() {
                await
                    RepositoryHandler.ReplaceRemotes(GetRemotes(_action.RemoteInfo, _getIsPremium()), _pm.Repo)
                        .ConfigureAwait(false);
                await _pm.UpdateRemotes().ConfigureAwait(false);
                _pm.StatusRepo.Reset(RepoStatus.Processing, 0);
            }

            public async Task<Package[]> Install(IDictionary<IPackagedContent, SpecificVersion> packagesToInstall,
                bool force, PackageProgress packageProgress, IAbsoluteDirectoryPath repoPath) {
                using (var repo = new Repository(repoPath, true)) {
                    SetupPackageManager(repo);
                    if (!_synqInitialized) {
                        await _pm.Repo.ClearObjectsAsync().ConfigureAwait(false);
                        await UpdateSynqRemotes().ConfigureAwait(false);
                        _synqInitialized = true;
                    }
                    _pm.Progress = packageProgress;
                    Package[] packages;
                    try {
                        packages = await _pm.ProcessPackages(packagesToInstall.Values, skipWhenFileMatches: !force)
                            .ConfigureAwait(false);
                    } catch (Exception) {
                        Failed.AddRange(packagesToInstall);
                        throw;
                    }
                    //HandlePackageStats(packagesToInstall);
                    Completed.AddRange(packagesToInstall);
                    return packages;
                }
            }

            /*
            private void HandlePackageStats(IDictionary<IPackagedContent, SpecificVersion> packagesToInstall) {
                var localPackageIndex = _pm.GetPackagesAsVersions();
                foreach (var p in packagesToInstall) {
                    if (localPackageIndex.ContainsKey(p.Value.Name)) {
                        if (localPackageIndex[p.Value.Name].Contains(p.Value.VersionInfo)) {
                            // already have version
                        } else {
                            HandleUpdateStats(p.Key, _action.Status);
                            // is Update
                        }
                    } else {
                        HandleInstallStats(p.Key, _action.Status);
                        // is Install
                    }
                }
            }
            */

            private void SetupPackageManager(Repository repo) {
                _pm = new PackageManager(repo, _action.Paths.Path, _statusRepo, true);
                Sync.Core.Packages.CheckoutType ct;
                if (!Enum.TryParse(_action.CheckoutType.ToString(), out ct))
                    throw new InvalidOperationException("Unsupported checkout type");
                _pm.Settings.CheckoutType = ct;
                _pm.Settings.GlobalWorkingPath = _action.GlobalWorkingPath;
            }

            private IEnumerable<KeyValuePair<Guid, Uri[]>> GetRemotes(RemoteInfoAttribute synqRemoteInfo, bool isPremium)
                => isPremium ? synqRemoteInfo.PremiumRemotes : synqRemoteInfo.DefaultRemotes;
        }

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

        public class SixSyncProgress
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

        class ExternalContentInstallerSession : Session
        {
            private readonly Dictionary<IPackagedContent, SpecificVersion> _content;
            private readonly IAbsoluteDirectoryPath _contentPath;
            private readonly ProgressLeaf[] _contentProgress;
            private readonly IExternalFileDownloader _dl;
            private readonly Game _game;

            public ExternalContentInstallerSession(IAbsoluteDirectoryPath contentPath,
                Dictionary<IPackagedContent, SpecificVersion> content, ProgressLeaf[] contentProgress, Game game,
                IExternalFileDownloader dl) {
                _contentPath = contentPath;
                _content = content;
                _contentProgress = contentProgress;
                _game = game;
                _dl = dl;
            }

            // TODO: Progress reporting
            public Task Install(CancellationToken cancelToken, bool force) {
                var i = 0;
                return RunAndThrow(_content.OrderBy(x => _game.GetPublisherUrl(x.Key).DnsSafeHost), async x => {
                    var f =
                        await
                            _dl.DownloadFile(_game.GetPublisherUrl(x.Key), _contentPath, _contentProgress[i++].Update,
                                cancelToken).ConfigureAwait(false);
                    var destinationDir = _contentPath.GetChildDirectoryWithName(x.Key.GetSource(_game).PublisherId);
                    if (destinationDir.Exists)
                        destinationDir.Delete(true);
                    if (f.IsArchive())
                        f.Unpack(destinationDir, true);
                    else {
                        destinationDir.Create();
                        f.Move(destinationDir);
                    }
                });
            }
        }
    }
}