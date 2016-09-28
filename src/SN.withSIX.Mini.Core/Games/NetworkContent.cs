// <copyright company="SIX Networks GmbH" file="NetworkContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using NDepend.Path;
using SN.withSIX.Mini.Core.Extensions;
using withSIX.Api.Models.Content;

namespace SN.withSIX.Mini.Core.Games
{
    public interface INetworkContent : IPackagedContent, IInstallableContent, IHavePath
    {
        ICollection<NetworkContentSpec> Dependencies { get; }
        ICollection<ContentPublisher> Publishers { get; }
    }

    public interface IHavePath
    {
        string ContentSlug { get; }
        string GetPath(string name);
    }

    public interface IHaveImage
    {
        Uri Image { get; }
    }

    [DataContract]
    public abstract class NetworkContent : PackagedContent, INetworkContent
    {
        private ContentPublisher _publisher;

        protected NetworkContent() {}

        protected NetworkContent(string packageName, Guid gameId) : base(packageName, gameId) {}

        [DataMember]
        // Only here because of wrong dependency references... // TODO: Try to externalize this to the DTO instead..
        public ICollection<ContentGuidSpec> InternalDependencies { get; protected set; } =
            new HashSet<ContentGuidSpec>();
        //[DataMember]
        //public List<string> Aliases { get; protected set; } = new HashSet<string>();
        [DataMember]
        public DateTime UpdatedVersion { get; set; }

        public override bool IsNetworkContent { get; } = true;

        [DataMember]
        public Guid? OriginalGameId { get; set; }
        [DataMember]
        public string OriginalGameSlug { get; set; }

        [DataMember]
        public ICollection<ContentPublisher> Publishers { get; protected set; } = new HashSet<ContentPublisher>();
        [IgnoreDataMember]
        public virtual ICollection<NetworkContentSpec> Dependencies { get; private set; } =
            new HashSet<NetworkContentSpec>();

        public override IAbsoluteDirectoryPath GetSourceDirectory(IHaveSourcePaths game)
            => GetSourceRoot(game).GetChildDirectoryWithName(GetSource(game).PublisherId);

        public string GetPath(string name) => this.GetContentPath(ContentSlug, name);

        public abstract string ContentSlug { get; }

        public override void OverrideSource(Publisher publisher)
            => _publisher = Publishers.First(x => x.Publisher == publisher);

        public override ContentPublisher GetSource(IHaveSourcePaths game)
            => _publisher ?? (_publisher = CalculatePublisher(game));

        protected override IContentSpec<Content> CreateRelatedSpec(string constraint)
            => new NetworkContentSpec(this, constraint ?? Version);

        protected override void HandleRelatedContentChildren(ICollection<IContentSpec<Content>> x) {
            foreach (var d in Dependencies)
                d.Content.GetRelatedContent(x, d.Constraint);
        }

        private IAbsoluteDirectoryPath GetSourceRoot(IHaveSourcePaths game)
            => GetSource(game).Publisher == Publisher.Steam
                ? game.SteamDirectories.Workshop.ContentPath
                : game.ContentPaths.Path;

        public void ReplaceDependencies(IEnumerable<NetworkContentSpec> dependencies)
            => Dependencies = dependencies.ToList();

        private ContentPublisher CalculatePublisher(IHaveSourcePaths game) {
            if (!Publishers.Any())
                throw new NotSupportedException("No supported Publishers found for: " + Id);
            if (Publishers.HasPublisher(Publisher.withSIX))
                return Publishers.GetPublisher(Publisher.withSIX);

            return Publishers.HasPublisher(Publisher.Steam) &&
                   ((Game.SteamHelper.SteamFound && game.SteamDirectories.IsValid) || (Publishers.Count == 1))
                ? Publishers.GetPublisher(Publisher.Steam)
                : Publishers.First(x => x.Publisher != Publisher.Steam);
        }

        [OnSerialized]
        void OnSerialized(StreamingContext context) {
            InternalDependencies =
                new HashSet<ContentGuidSpec>(Dependencies.GroupBy(x => x.Content.Id).Select(x => x.First())
                    .Select(x => new ContentGuidSpec(x.Content.Id, x.Constraint)));
        }

        public void UpdateVersionInfo(string version, DateTime updatedVersion) {
            Contract.Requires<ArgumentNullException>(version != null);
            if (Version == version)
                return;
            Version = version;
            UpdatedVersion = updatedVersion;
            // TODO: This does not actually refresh the Network Version information in the UI's..
            UpdateState();
        }
    }
}