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
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace SN.withSIX.Mini.Core.Games
{
    public interface IContent : IHaveGameId, IHaveId<Guid>
    {
        string Name { get; set; }
        bool IsFavorite { get; set; }
        string Version { get; }
        InstallInfo InstallInfo { get; }
        RecentInfo RecentInfo { get; }
        ItemState GetState();
        ItemState GetState(string constraint);
        void Installed(string version, bool completed);
        IEnumerable<ILaunchableContent> GetLaunchables(string constraint = null);
        Task PostInstall(IInstallerSession installerSession, CancellationToken cancelToken, bool processed);
        void RegisterAdditionalPostInstallTask(Func<bool, Task> task);
        void Use(LaunchType launchType = LaunchType.Default);
    }

    public interface IHavePackageName
    {
        string PackageName { get; set; }
    }

    public interface IContentWithPackageName : IHavePackageName, IContent { }

    public interface IPackagedContent : IContentWithPackageName, IUninstallableContent {}

    public interface IModContent : IPackagedContent, ILaunchableContent {}

    public interface IMissionContent : IPackagedContent, ILaunchableContent {}

    public interface ICollectionContent : ILaunchableContent, IContent {}

    public interface IInstallableContent : IContent
    {
        Task Install(IInstallerSession installerSession, CancellationToken cancelToken, string constraint = null);

        IEnumerable<IContentSpec<Content>> GetRelatedContent(List<IContentSpec<Content>> list = null,
            string constraint = null);

        void SetIncomplete(string constraint);
    }

    public interface IUninstallableContent : IContent
    {
        Task Uninstall(IUninstallSession contentInstaller, CancellationToken cancelToken, string constraint = null);
    }

    [DataContract]
    public abstract class Content : BaseEntityGuidId, IContent
    {
        protected Content() {}

        protected Content(string name, Guid gameId) : this() {
            Contract.Requires<ArgumentNullException>(name != null);
            Name = name;
            GameId = gameId;
        }

        [DataMember]
        public Uri Image { get; set; }
        [DataMember]
        public string Author { get; set; }
        [IgnoreDataMember]
        List<Func<bool, Task>> AdditionalPostInstallActions { get; } = new List<Func<bool, Task>>();

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
        public Guid GameId { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public bool IsFavorite { get; set; }
        [DataMember]
        public string Version { get; protected set; }

        public IEnumerable<ILaunchableContent> GetLaunchables(string constraint = null)
            => GetRelatedContent(constraint: constraint).Select(x => x.Content).OfType<ILaunchableContent>();

        public virtual async Task PostInstall(IInstallerSession installerSession, CancellationToken cancelToken,
            bool processed) {
            foreach (var a in AdditionalPostInstallActions)
                await a(processed).ConfigureAwait(false);
        }

        public void RegisterAdditionalPostInstallTask(Func<bool, Task> task) => AdditionalPostInstallActions.Add(task);

        public ItemState GetState() => State ?? InitialCachedState();

        public ItemState GetState(string constraint)
            => constraint == null || Version == constraint ? GetState() : CalculateState(constraint);

        public void Use(LaunchType launchType = LaunchType.Default) {
            RecentInfo = new RecentInfo(launchType);
            PrepareEvent(new ContentUsed(this));
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
                $"$$$ CalculateState [{Id}] {Name} for desired v{GetWithDesired(desiredVersion)}, Installed: {installedVersion}, Complete: {complete}. Result: {result}");
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
                $"$$$ HasUpdate [{Id}] {Name} for {GetWithDesired(desiredVersion)}, isLatestVersion: {isLatestVersion}. Result: {result}");
        }

        private bool IsLatestVersion(string desiredVersion) => InstallInfo.Version == desiredVersion;

        public virtual IEnumerable<string> GetContentNames() {
            yield return Name;
        }

        public abstract IEnumerable<IContentSpec<Content>> GetRelatedContent(List<IContentSpec<Content>> list = null,
            string constraint = null);

        public void MakeFavorite() {
            IsFavorite = true;
            PrepareEvent(new ContentFavorited(this));
        }

        public void Unfavorite() {
            IsFavorite = false;
            PrepareEvent(new ContentUnFavorited(this));
        }

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
    }

    public class RecentItemRemoved : IDomainEvent
    {
        public RecentItemRemoved(Content content) {
            Content = content;
        }

        public Content Content { get; }
    }

    public class ContentFavorited : IDomainEvent
    {
        public ContentFavorited(Content content) {
            Content = content;
        }

        public Content Content { get; }
    }

    public class ContentUnFavorited : IDomainEvent
    {
        public ContentUnFavorited(Content content) {
            Content = content;
        }

        public Content Content { get; }
    }

    public interface ILaunchableContent : IContent {}

    [DataContract]
    public abstract class InstallableContent : Content, IInstallableContent
    {
        protected InstallableContent(string name, Guid gameId) : base(name, gameId) {}
        protected InstallableContent() {}
        // TODO: We only call Install on top-level entities, like a collection, or like the top of a dependency tree
        // PostInstall is however called for every processed entity now...
        public virtual Task Install(IInstallerSession installerSession, CancellationToken cancelToken,
            string constraint = null) => installerSession.Install(GetPackaged(constraint).ToArray());

        public void SetIncomplete(string constraint) {
            Installed(constraint, false);
        }

        protected IEnumerable<IContentSpec<IPackagedContent>> GetPackaged(string constraint = null)
            => GetRelatedContent(constraint: constraint)
                .OfType<IContentSpec<IPackagedContent>>();
    }
}