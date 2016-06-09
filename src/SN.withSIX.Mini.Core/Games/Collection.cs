// <copyright company="SIX Networks GmbH" file="Collection.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Core.Extensions;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace SN.withSIX.Mini.Core.Games
{
    [DataContract]
    public abstract class Collection : InstallableContent, ICollectionContent, IUninstallableContent
    {
        protected Collection() {}
        protected Collection(string name, Guid gameId) : base(name, gameId) {}
        // TODO: Handle circular?
        [DataMember]
        public virtual ICollection<CollectionContentSpec> Dependencies { get; protected set; } =
            new List<CollectionContentSpec>();
        // TODO: Client vs Server vs All ?
        // TODO: Optional vs Required ?
        [DataMember]
        public virtual ICollection<ContentSpec> Contents { get; protected set; } =
            new List<ContentSpec>();

        public async Task Uninstall(IUninstallSession contentInstaller, CancellationToken cancelToken,
            string constraint = null) {
            await contentInstaller.UninstallCollection(this, cancelToken, constraint).ConfigureAwait(false);
            RemoveRecentInfo();
        }

        // A collection is considered installed when the installation of the collection completed for the first time
        // When a collection changes, e.g a mod has been added, or updated, the collection is considered to Have Updates (Rather than not being installed)
        protected override bool HasUpdate(string desiredVersion = null)
            => base.HasUpdate(desiredVersion) || ContentsIsNotUptodate();

        private bool ContentsIsNotUptodate() {
            var value = OriginalContentIsNotUptodate();
            if (Common.Flags.Verbose)
                LogContentsIsNotUptodate(value);
            return value;
        }

        private bool OriginalContentIsNotUptodate() => Contents.Any(x => x.GetState().IsNotUptodate());

        private void LogContentsIsNotUptodate(bool result) {
            var notUpdate = string.Join(", ",
                Contents.Where(x => x.GetState().IsNotUptodate()).Select(x => x.Content.Id));
            MainLog.Logger.Info($"$$$ ContentsIsNotUptodate [{Id}] {Name}: {notUpdate}. Result: {result}");
        }

        protected IEnumerable<IContentSpec<Collection>> GetCollections(string constraint = null)
            => GetRelatedContent(constraint: constraint).OfType<IContentSpec<Collection>>();

        public override async Task Install(IInstallerSession installerSession, CancellationToken cancelToken,
            string constraint = null) {
            await base.Install(installerSession, cancelToken, constraint).ConfigureAwait(false);
            foreach (var c in GetCollections(constraint))
                await c.Content.PostInstall(installerSession, cancelToken).ConfigureAwait(false);
            Installed(constraint ?? Version, true);
        }

        public override void UpdateState(bool force = true) {
            if (!IsInstalled()) {
                if (!ContentsIsNotUptodate())
                    Installed(Version, true);
            }
            base.UpdateState(force);
        }

        public override IEnumerable<IContentSpec<Content>> GetRelatedContent(List<IContentSpec<Content>> list = null,
            string constraint = null) {
            if (list == null)
                list = new List<IContentSpec<Content>>();

            if (list.Select(x => x.Content).Contains(this))
                return list;

            var spec = new CollectionContentSpec(this, constraint);
            list.Add(spec);

            foreach (var d in Dependencies)
                d.Content.GetRelatedContent(list, d.Constraint);

            foreach (var c in Contents)
                c.Content.GetRelatedContent(list, c.Constraint);

            // Workaround for top level version overrides should take precedence.
            foreach (var c in Contents) {
                var e = list.First(x => x.Content == c.Content);
                if (c.Constraint != null)
                    e.Constraint = c.Constraint;
            }

            list.RemoveAll(x => x.Content == this);
            list.Add(spec);

            return list;
        }

        public override IEnumerable<string> GetContentNames() => Contents.Select(x => x.Content.Name);
    }

    [DataContract]
    public class LocalCollection : Collection
    {
        public LocalCollection(Guid gameId, string name, ICollection<ContentSpec> contents) : base(name, gameId) {
            Contract.Requires<ArgumentNullException>(contents != null);
            //Author = "You"; // better assume null author = you?
            Contents = contents;
            UpdateFromContents();
            var version = Version = "0.0.1";
            InstallInfo = new InstallInfo(Size, SizePacked, version);
        }

        private void UpdateFromContents() {
            var ag =
                GetRelatedContent()
                    .Select(x => x.Content)
                    .Distinct()
                    .Aggregate(Tuple.Create<long, long>(0, 0),
                        (cur, x) => Tuple.Create(cur.Item1 + x.Size, cur.Item2 + x.SizePacked));
            Size = ag.Item1;
            SizePacked = ag.Item2;
        }
    }

    [DataContract]
    public abstract class NetworkCollection : Collection, IHaveRepositories, IHaveServers, ILaunchableContent, IHavePath
        // Hmm ILaunchableContent.. (is to allow SErvers to be collected from this collection, not sure if best)
    {
        protected NetworkCollection(Guid id, string name, Guid gameId) {
            Id = id;
            Name = name;
            GameId = gameId;
        }

        public override bool IsNetworkContent { get; } = true;

        public string GetPath() => this.GetContentPath(ContentSlug);

        public virtual string ContentSlug { get; } = "collections";
        [DataMember]
        public virtual ICollection<string> Repositories { get; protected set; } = new List<string>();
        [DataMember]
        public virtual ICollection<CollectionServer> Servers { get; protected set; } = new List<CollectionServer>();

        public bool GetHasServers() => Servers.Any();

        public int GetModsCount() => Contents.Count;
    }

    [DataContract]
    public class SubscribedCollection : NetworkCollection, IHaveGroup
    {
        public SubscribedCollection(Guid id, string name, Guid gameId) : base(id, name, gameId) {}
    }

    [DataContract]
    public class CollectionServer
    {
        [DataMember]
        // ServerAddress
        public string Address { get; set; }
        [DataMember]
        public string Password { get; set; }
    }

    public interface IHaveServers
    {
        ICollection<CollectionServer> Servers { get; }
    }

    public interface IHaveRepositories
    {
        ICollection<string> Repositories { get; }
    }

    public interface IHaveGroup
    {
        Guid? GroupId { get; set; }
    }
}