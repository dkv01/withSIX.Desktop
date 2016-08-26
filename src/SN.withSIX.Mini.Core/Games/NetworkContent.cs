// <copyright company="SIX Networks GmbH" file="NetworkContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NDepend.Path;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Extensions;
using withSIX.Api.Models.Content;

namespace SN.withSIX.Mini.Core.Games
{
    public interface INetworkContent : IPackagedContent, IInstallableContent, IHavePath
    {
        ICollection<NetworkContentSpec> Dependencies { get; }
        List<ContentPublisher> Publishers { get; }
    }

    public interface IHavePath
    {
        string ContentSlug { get; }
        string GetPath();
    }

    public interface IHaveImage
    {
        Uri Image { get; }
    }

    [DataContract]
    public abstract class NetworkContent : PackagedContent, INetworkContent
    {
        private readonly Lazy<ContentPublisher> _source;

        protected NetworkContent() {
            _source = SystemExtensions.CreateLazy(GetSource);
        }

        protected NetworkContent(string name, string packageName, Guid gameId) : base(name, packageName, gameId) {
            _source = SystemExtensions.CreateLazy(GetSource);
        }

        [DataMember]
        // Only here because of wrong dependency references... // TODO: Try to externalize this to the DTO instead..
        public ICollection<ContentGuidSpec> InternalDependencies { get; protected set; } = new List<ContentGuidSpec>();
        //[DataMember]
        //public List<string> Aliases { get; protected set; } = new List<string>();
        [DataMember]
        public DateTime UpdatedVersion { get; set; }

        public override bool IsNetworkContent { get; } = true;

        [DataMember]
        public Guid? OriginalGameId { get; set; }
        [DataMember]
        public string OriginalGameSlug { get; set; }

        [DataMember]
        public List<ContentPublisher> Publishers { get; protected set; } = new List<ContentPublisher>();
        [IgnoreDataMember]
        public virtual ICollection<NetworkContentSpec> Dependencies { get; private set; } =
            new List<NetworkContentSpec>();

        public override IAbsoluteDirectoryPath GetSourceDirectory(IHaveSourcePaths game)
            => GetSourceRoot(game).GetChildDirectoryWithName(Source.PublisherId);

        public string GetPath() => this.GetContentPath(ContentSlug);

        public abstract string ContentSlug { get; }

        public override IEnumerable<IContentSpec<Content>> GetRelatedContent(List<IContentSpec<Content>> list = null,
            string constraint = null) {
            if (list == null)
                list = new List<IContentSpec<Content>>();

            if (list.Select(x => x.Content).Contains(this))
                return list;

            var spec = new NetworkContentSpec(this, constraint ?? Version);
            list.Add(spec);
            foreach (var d in Dependencies)
                d.Content.GetRelatedContent(list, d.Constraint);
            list.RemoveAll(x => x.Content == this);
            list.Add(spec);

            return list;
        }

        public override ContentPublisher Source => _source.Value;

        private IAbsoluteDirectoryPath GetSourceRoot(IHaveSourcePaths game) => Source.Publisher == Publisher.Steam
            ? game.SteamworkshopPaths.ContentPath
            : game.ContentPaths.Path;

        public void ReplaceDependencies(IEnumerable<NetworkContentSpec> dependencies)
            => Dependencies = dependencies.ToList();

        private ContentPublisher GetSource() {
            if (!Publishers.Any())
                throw new NotSupportedException("No supported Publishers found for: " + Id);
            if (Publishers.HasPublisher(Publisher.withSIX))
                return Publishers.GetPublisher(Publisher.withSIX);
            return Publishers.HasPublisher(Publisher.Steam) && (Game.SteamHelper.SteamFound || Publishers.Count == 1)
                ? Publishers.GetPublisher(Publisher.Steam)
                : Publishers.First(x => x.Publisher != Publisher.Steam);
        }

        [OnSerialized]
        void OnSerialized(StreamingContext context) {
            InternalDependencies =
                Dependencies.Select(x => new ContentGuidSpec(x.Content.Id, x.Constraint)).ToList();
        }

        public void UpdateVersionInfo(string version, DateTime updatedVersion) {
            if (Version == version)
                return;
            Version = version;
            UpdatedVersion = updatedVersion;
            // TODO: This does not actually refresh the Network Version information in the UI's..
            UpdateState();
        }
    }
}