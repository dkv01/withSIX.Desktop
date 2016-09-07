// <copyright company="SIX Networks GmbH" file="Content.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Core.Extensions;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Api.Models;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.v3;

namespace SN.withSIX.Mini.Core.Games
{
    [ContractClassFor(typeof (IContent))]
    public abstract class IContentContract : IContent
    {
        private string _name;
        public abstract bool IsFavorite { get; set; }
        public string Name
        {
            get { return _name; }
            set
            {
                Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(value));
                _name = value;
            }
        }
        public abstract Guid GameId { get; }
        public abstract Guid Id { get; }
        public abstract string Version { get; }
        public abstract ItemState ProcessingState { get; }
        public abstract void StartProcessingState(string version, bool force);
        public abstract void FinishProcessingState(string version, bool completed);
        public abstract void CancelProcessingState();
        public abstract InstallInfo InstallInfo { get; }
        public abstract RecentInfo RecentInfo { get; }
        public abstract ItemState GetState();
        public abstract ItemState GetState(string constraint);
        public abstract void Installed(string version, bool completed);
        public abstract IEnumerable<ILaunchableContent> GetLaunchables(string constraint = null);

        public abstract Task PostInstall(IInstallerSession installerSession, CancellationToken cancelToken,
            bool processed);

        public abstract Task PreUninstall(IUninstallSession installerSession, CancellationToken cancelToken, bool processed);

        public abstract void RegisterAdditionalPostInstallTask(Func<bool, Task> task);
        public abstract void Use(IContentAction<IContent> action);
        public abstract void Use(ILaunchContentAction<IContent> action);
    }

    public interface IProcessingState
    {
        ItemState ProcessingState { get; }
        void StartProcessingState(string version, bool force);
        void FinishProcessingState(string version, bool completed);
        void CancelProcessingState();
    }

    [ContractClass(typeof (IContentContract))]
    public interface IContent : IHaveGameId, IHaveId<Guid>, IPostInstallable, IProcessingState
    {
        string Version { get; }

        InstallInfo InstallInfo { get; }
        RecentInfo RecentInfo { get; }
        ItemState GetState();
        ItemState GetState(string constraint);
        IEnumerable<ILaunchableContent> GetLaunchables(string constraint = null);
        void Use(IContentAction<IContent> action);
        void Use(ILaunchContentAction<IContent> action);
    }

    [ContractClass(typeof (IHavePackageNameContract))]
    public interface IHavePackageName
    {
        string PackageName { get; set; }
        string GetFQN(string constraint = null);
    }

    [ContractClassFor(typeof (IHavePackageName))]
    public abstract class IHavePackageNameContract : IHavePackageName
    {
        private string _packageName;
        public string PackageName
        {
            get { return _packageName; }
            set
            {
                Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(value));
                _packageName = value;
            }
        }
        public abstract string GetFQN(string constraint = null);
    }

    public static class ContentExtensions
    {
        public static ItemState GetState(this IContent This, SpecificVersionInfo constraint) => This.GetState(constraint?.ToString());
        public static ItemState GetState(this IContent This, SpecificVersion constraint) => This.GetState(constraint?.VersionInfo);
    }

    public interface ISourcedContent
    {
        ContentPublisher GetSource(IHaveSourcePaths game);
        IAbsoluteDirectoryPath GetSourceDirectory(IHaveSourcePaths game);
        void OverrideSource(Publisher publisher);
    }

    public interface IContentWithPackageName : IHavePackageName, IContent, ISourcedContent
    {
    }

    public interface IPackagedContent : IContentWithPackageName, IUninstallableContent { }

    public interface IModContent : IPackagedContent, ILaunchableContent {}

    public interface IMissionContent : IPackagedContent, ILaunchableContent {}

    public interface ICollectionContent : ILaunchableContent, IContent {}

    public interface IPostInstallable
    {
        Task PostInstall(IInstallerSession installerSession, CancellationToken cancelToken, bool processed);
        void RegisterAdditionalPostInstallTask(Func<bool, Task> task);
        void Installed(string version, bool completed);
    }

    public interface IInstallableContent : IContent
    {
        Task Install(IInstallerSession installerSession, CancellationToken cancelToken, string constraint = null);

        IEnumerable<IContentSpec<Content>> GetRelatedContent(string constraint = null);

        void SetIncomplete(string constraint);
    }

    public interface IUninstallableContent : IContent
    {
        Task Uninstall(IUninstallSession contentInstaller, CancellationToken cancelToken, string constraint = null);
        Task PreUninstall(IUninstallSession installerSession, CancellationToken cancelToken, bool processed);
    }

    [DataContract]
    public abstract class Content : BaseEntityGuidId, IContent
    {
        private Guid _gameId;
        protected Content() {}

        protected Content(Guid gameId) : this() {
            //Contract.Requires<ArgumentException>(gameId != Guid.Empty);
            _gameId = gameId; // circumvent the setter protection
        }

        [IgnoreDataMember]
        List<Func<bool, Task>> AdditionalPostInstallActions { get; } = new List<Func<bool, Task>>();

        [IgnoreDataMember]
        List<Func<bool, Task>> AdditionalPreUninstallActions { get; } = new List<Func<bool, Task>>();

        [DataMember]
        public Guid? GroupId { get; set; }

        public virtual bool IsNetworkContent { get; } = false;
        [DataMember]
        public long Size { get; set; }
        [DataMember]
        public long SizePacked { get; set; }

        // TODO: Lets cache this another day!
        [IgnoreDataMember]
        protected ItemState? State { get; set; }
        [DataMember]
        public RecentInfo RecentInfo { get; protected set; }

        public void Installed(string version, bool completed) {
            if (!IsInstalled())
                JustInstalled(version, completed);
            else {
                if (InstallInfo.Version != version || InstallInfo.Completed != completed)
                    ChangeVersion(version, completed);
            }
        }

        [DataMember]
        public InstallInfo InstallInfo { get; protected set; }
        [DataMember]
        public Guid GameId
        {
            get { return _gameId; }
            protected set
            {
                if (value == Guid.Empty)
                    throw new ArgumentException(nameof(value));
                _gameId = value;
            }
        }
        [DataMember]
        public string Version { get; protected set; }

        public IEnumerable<ILaunchableContent> GetLaunchables(string constraint = null)
            => GetRelatedContent(constraint).Select(x => x.Content).OfType<ILaunchableContent>();

        public virtual async Task PostInstall(IInstallerSession installerSession, CancellationToken cancelToken,
            bool processed) {
            foreach (var a in AdditionalPostInstallActions)
                await a(processed).ConfigureAwait(false);
        }

        public virtual async Task PreUninstall(IUninstallSession installerSession, CancellationToken cancelToken,
            bool processed) {
            foreach (var a in AdditionalPreUninstallActions)
                await a(processed).ConfigureAwait(false);
        }

        public void RegisterAdditionalPostInstallTask(Func<bool, Task> task) => AdditionalPostInstallActions.Add(task);

        public void RegisterAdditionalPreUninstallTask(Func<bool, Task> task)
            => AdditionalPreUninstallActions.Add(task);

        public ItemState GetState() => State ?? InitialCachedState();

        public ItemState GetState(string constraint)
            => constraint == null || Version == constraint ? GetState() : CalculateState(constraint);

        [IgnoreDataMember]
        public ItemState ProcessingState { get; private set; }

        public void StartProcessingState(string constraint, bool force) {
            ProcessingState = GetProcessingState(constraint, force);
        }

        public void CancelProcessingState() {
            ProcessingState = GetState();
        }

        public void FinishProcessingState(string version, bool completed) {
            var state = GetState(version);
            Installed(version, completed);
            if (state != GetState(version))
                UpdateState(true);
            ProcessingState = GetState();
        }

        ItemState GetProcessingState(string constraint, bool force) {
            if (force)
                return ItemState.Diagnosing;
            var processState = GetState(constraint);
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

        public void Use(IContentAction<IContent> action) {
            RecentInfo = new RecentInfo();
            PrepareEvent(new ContentUsed(this, action));
        }

        public void Use(ILaunchContentAction<IContent> action) {
            RecentInfo = new RecentInfo(action.LaunchType);
            PrepareEvent(new ContentUsed(this, action));
        }

        public bool IsInstalled() => InstallInfo != null && InstallInfo.Completed;

        void ChangeVersion(string version, bool completed) {
            InstallInfo.Updated(version, Size, SizePacked, completed);
            UpdateState();
            // TODO: What about updating the version info in the StateHandler cache etc??
        }

        void JustInstalled(string version, bool completed) {
            InstallInfo = new InstallInfo(Size, SizePacked, version, completed);
            UpdateState();
        }

        void RaiseContentStatusChanged() => PrepareEvent(new ContentStatusChanged(this, State.GetValueOrDefault()));

        private ItemState InitialCachedState() {
            var state = CalculateState(null);
            State = state;
            return state;
        }

        // TODO: Normally we wouldn't need to calculate if we would have an initial migration that saves the states ?

        public virtual void UpdateState(bool force = true) {
            var currentState = State;
            State = CalculateState(null);
            // TODO: There should be more considerations, because on the statuschanged event
            // we carry more data, like current installed version, and such!
            if (!force && currentState == State)
                return;
            RaiseContentStatusChanged();
        }

        protected ItemState CalculateState(string desiredVersion) {
            var value = OriginalCalculateState(desiredVersion);
            if (Common.Flags.Verbose)
                LogCalculateState(desiredVersion, value);
            return value;
        }

        internal bool IsIncompleteInstalled() => InstallInfo != null && !InstallInfo.Completed;

        private ItemState OriginalCalculateState(string desiredVersion) {
            if (IsIncompleteInstalled())
                return ItemState.Incomplete;
            if (!IsInstalled())
                return ItemState.NotInstalled;
            return HasUpdate(desiredVersion) ? ItemState.UpdateAvailable : ItemState.Uptodate;
        }

        private void LogCalculateState(string desiredVersion, ItemState result) {
            var isInstalled = IsInstalled();
            var installedVersion = !isInstalled ? "No" : InstallInfo.Version;
            var complete = isInstalled && InstallInfo.Completed;
            MainLog.Logger.Info(
                $"$$$ CalculateState [{Id}] {(this as IHavePackageName)?.PackageName} for desired v{GetWithDesired(desiredVersion)}, Installed: {installedVersion}, Complete: {complete}. Result: {result}");
        }

        protected string GetWithDesired(string desiredVersion)
            => desiredVersion == null ? Version : $"{desiredVersion}/{Version}";

        protected virtual bool HasUpdate(string desiredVersion = null) {
            var value = OriginalHasUpdate(desiredVersion);
            if (Common.Flags.Verbose)
                LogHasUpdate(desiredVersion, value);
            return value;
        }

        private bool OriginalHasUpdate(string desiredVersion) {
            if (desiredVersion == null)
                desiredVersion = Version;
            return IsInstalled() && !IsLatestVersion(desiredVersion);
        }

        private void LogHasUpdate(string desiredVersion, bool result) {
            var isInstalled = IsInstalled();
            var isLatestVersion = isInstalled && IsLatestVersion(desiredVersion ?? Version);
            MainLog.Logger.Info(
                $"$$$ HasUpdate [{Id}] {(this as IHavePackageName)?.PackageName} for {GetWithDesired(desiredVersion)}, isLatestVersion: {isLatestVersion}. Result: {result}");
        }

        private bool IsLatestVersion(string desiredVersion)
            =>
                InstallInfo.Version?.Equals(desiredVersion, StringComparison.CurrentCultureIgnoreCase) ??
                InstallInfo.Version == desiredVersion;

        public IEnumerable<IContentSpec<Content>> GetRelatedContent(string constraint = null) {
            var l = new List<IContentSpec<Content>>();
            GetRelatedContent(l, constraint);
            return l;
        }

        internal void GetRelatedContent(ICollection<IContentSpec<Content>> l, string constraint)
            =>
                l.BuildDependencies(() => CreateRelatedSpec(constraint), x => x.Select(c => c.Content).Contains(this),
                    HandleRelatedContentChildren);

        protected abstract IContentSpec<Content> CreateRelatedSpec(string constraint);

        protected abstract void HandleRelatedContentChildren(ICollection<IContentSpec<Content>> x);

        //internal abstract void GetRelatedContent(List<IContentSpec<Content>> list, string constraint = null);

        public void RemoveRecentInfo() {
            RemoveRecentInfoInternal();
            UpdateState();
        }

        private void RemoveRecentInfoInternal() {
            RecentInfo = null;
            PrepareEvent(new RecentItemRemoved(this));
        }

        public void Uninstalled() {
            var previousInstallInfo = InstallInfo;
            if (previousInstallInfo != null)
                InstallInfo = null;

            var previousRecentInfo = RecentInfo;
            if (previousRecentInfo != null)
                RemoveRecentInfoInternal();
            if (previousRecentInfo != null || previousInstallInfo != null)
                UpdateState();
        }

        public void FixGameId(Guid id) {
            GameId = id;
        }
    }

    public class RecentItemRemoved : ISyncDomainEvent
    {
        public RecentItemRemoved(Content content) {
            Content = content;
        }

        public Content Content { get; }
    }

    public interface ILaunchableContent : IContent {}

    [DataContract]
    public abstract class InstallableContent : Content, IInstallableContent
    {
        protected InstallableContent(Guid gameId) : base(gameId) {}
        protected InstallableContent() {}
        // TODO: We only call Install on top-level entities, like a collection, or like the top of a dependency tree
        // PostInstall is however called for every processed entity now...
        public virtual Task Install(IInstallerSession installerSession, CancellationToken cancelToken,
            string constraint = null) => installerSession.Install(GetPackaged(constraint).ToArray());

        public void SetIncomplete(string constraint) => Installed(constraint, false);

        protected IEnumerable<IContentSpec<IPackagedContent>> GetPackaged(string constraint = null)
            => GetRelatedContent(constraint).OfType<IContentSpec<IPackagedContent>>();
    }
}