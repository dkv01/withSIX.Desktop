// <copyright company="SIX Networks GmbH" file="PackagedContent.cs">
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
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Api.Models.Content;

namespace SN.withSIX.Mini.Core.Games
{
    [DataContract]
    public abstract class PackagedContent : InstallableContent, IPackagedContent
    {
        private readonly Lazy<ContentPublisher> _source;

        protected PackagedContent() {
            _source = SystemExtensions.CreateLazy(() => new ContentPublisher(Publisher.withSIX, PackageName));
        }

        protected PackagedContent(string packageName, Guid gameId) : base(gameId) {
            Contract.Requires<ArgumentNullException>(packageName != null);
            Contract.Requires<ArgumentOutOfRangeException>(!string.IsNullOrWhiteSpace(packageName));
            PackageName = packageName;
            _source = SystemExtensions.CreateLazy(() => new ContentPublisher(Publisher.withSIX, PackageName));
        }

        [DataMember]
        public string PackageName { get; set; }
        public string GetFQN(string constraint = null) => PackageName.ToLower() + "-" + (constraint ?? Version);

        public virtual ContentPublisher GetSource(IHaveSourcePaths game) => _source.Value;
        public virtual void OverrideSource(Publisher publisher) { }

        public virtual IAbsoluteDirectoryPath GetSourceDirectory(IHaveSourcePaths game)
            => game.ContentPaths.Path.GetChildDirectoryWithName(PackageName);

        public Task Uninstall(IUninstallSession installerSession, CancellationToken cancelToken,
            string constraint = null) => installerSession.Uninstall(this);

        public override IEnumerable<IContentSpec<Content>> GetRelatedContent(List<IContentSpec<Content>> list = null,
            string constraint = null) {
            if (list == null)
                list = new List<IContentSpec<Content>>();

            if (list.Select(x => x.Content).Contains(this))
                return list;

            var spec = new PackagedContentSpec(this, constraint ?? Version);
            list.Add(spec);
            return list;
        }
    }
}