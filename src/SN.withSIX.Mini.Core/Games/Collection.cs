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
        protected Collection(Guid gameId, string name) : base(gameId) {
            Name = name;
        }

        [DataMember]
        public string Name { get; protected set; }

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

        public void ReplaceContent(IEnumerable<ContentSpec> contents)
            => Contents = contents.ToList();

        // A collection is considered installed when the installation of the collection completed for the first time
        // When a collection changes, e.g a mod has been added, or updated, the collection is considered to Have Updates (Rather than not being installed)
        protected override bool HasUpdate(string desiredVersion = null)
            => base.HasUpdate(desiredVersion) || !ContentIsUptodate();

        private bool ContentIsInstalled() => GetRelatedContent().All(x => x.Content.IsInstalled());

        private bool ContentIsUptodate() {
            var value = OriginalContentIsUptodate();
            if (Common.Flags.Verbose)
                LogContentIsUptodate(value);
            return value;
        }

        private bool OriginalContentIsUptodate() => Contents.All(x => !x.GetState().RequiresAction());

        private void LogContentIsUptodate(bool result) {
            var requiresAction = string.Join(", ",
                Contents.Where(x => x.GetState().RequiresAction()).Select(x => x.Content.Id));
            MainLog.Logger.Info($"$$$ ContentsIsUptodate [{Id}]: Todos: {requiresAction}. Result: {result}");
        }

        protected IEnumerable<IContentSpec<Collection>> GetCollections(string constraint = null)
            => GetRelatedContent(constraint).OfType<IContentSpec<Collection>>();

        public override async Task Install(IInstallerSession installerSession, CancellationToken cancelToken,
            string constraint = null) {
            await base.Install(installerSession, cancelToken, constraint).ConfigureAwait(false);
            //foreach (var c in GetCollections(constraint))
                //await c.Content.PostInstall(installerSession, cancelToken, true).ConfigureAwait(false);
            Installed(constraint ?? Version, true);
        }

        public override void UpdateState(bool force = true) {
            if (!IsInstalled()) {
                if (ContentIsInstalled())
                    Installed(Version, true);
            }
            base.UpdateState(force);
        }

        protected override IContentSpec<Content> CreateRelatedSpec(string constraint) => new CollectionContentSpec(this, constraint ?? Version);

        protected override void HandleRelatedContentChildren(ICollection<IContentSpec<Content>> x) => ProcessDependenciesFirstThenOurContents(x);

        private void ProcessDependenciesFirstThenOurContents(ICollection<IContentSpec<Content>> list) {
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
        }

        private void ProcessOurContentsFirstThenDependencies(ICollection<IContentSpec<Content>> list) {
            // First process the Contents, so that our constraints take precedence,
            // however our 'Dependencies' aren't really our Dependencies anymore then..
            foreach (var c in Contents)
                c.Content.GetRelatedContent(list, c.Constraint);

            foreach (var d in Dependencies)
                d.Content.GetRelatedContent(list, d.Constraint);
        }

        public void Replace(ContentSpec existing, Content n) {
            Contents.Remove(existing);
            Contents.Add(new ContentSpec(n, existing.Constraint));
        }
    }

    [DataContract]
    public class LocalCollection : Collection
    {
        public LocalCollection(Guid gameId, string name, ICollection<ContentSpec> contents) : base(gameId, name) {
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
        protected NetworkCollection(Guid id, Guid gameId, string name) : base(gameId, name) {
            Id = id;
        }

        public override bool IsNetworkContent { get; } = true;

        public string GetPath(string name) => this.GetContentPath(ContentSlug, name);

        public virtual string ContentSlug { get; } = "collections";
        [DataMember]
        public virtual ICollection<string> Repositories { get; protected set; } = new HashSet<string>();
        [DataMember]
        public virtual ICollection<CollectionServer> Servers { get; protected set; } = new HashSet<CollectionServer>();

        public bool GetHasServers() => Servers.Any();

        public int GetModsCount() => Contents.Count;
    }

    [DataContract]
    public class SubscribedCollection : NetworkCollection, IHaveGroup
    {
        public SubscribedCollection(Guid id, Guid gameId, string name) : base(id, gameId, name) {}
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