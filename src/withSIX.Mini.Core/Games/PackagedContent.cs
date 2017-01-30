// <copyright company="SIX Networks GmbH" file="PackagedContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models.Content;
using withSIX.Core.Extensions;
using withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace withSIX.Mini.Core.Games
{
    [DataContract]
    public abstract class PackagedContent : InstallableContent, IPackagedContent
    {
        private readonly Lazy<ContentPublisher> _source;
        private string _packageName;

        protected PackagedContent() {
            _source = SystemExtensions.CreateLazy(() => new ContentPublisher(Publisher.withSIX, PackageName));
        }

        protected PackagedContent(string packageName, Guid gameId) : base(gameId) {
            if (packageName == null) throw new ArgumentNullException(nameof(packageName));
            if (!(!string.IsNullOrWhiteSpace(packageName))) throw new ArgumentOutOfRangeException("!string.IsNullOrWhiteSpace(packageName)");
            PackageName = packageName;
            _source = SystemExtensions.CreateLazy(() => new ContentPublisher(Publisher.withSIX, PackageName));
        }

        [DataMember]
        public string PackageName
        {
            get { return _packageName; }
            set
            {
                if (!(!string.IsNullOrWhiteSpace(value))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(value)");
                _packageName = value;
            }
        }
        public string GetFQN(string constraint = null) => PackageName.ToLower() + "-" + (constraint ?? Version);

        public virtual ContentPublisher GetSource(IHaveSourcePaths game) => _source.Value;
        public virtual void OverrideSource(Publisher publisher) {}

        public virtual IAbsoluteDirectoryPath GetSourceDirectory(IHaveSourcePaths game)
            => game.ContentPaths.Path.GetChildDirectoryWithName(PackageName);

        public Task Uninstall(IUninstallSession installerSession, CancellationToken cancelToken,
            string constraint = null) => installerSession.Uninstall(this);

        protected override IContentSpec<Content> CreateRelatedSpec(string constraint)
            => new PackagedContentSpec(this, constraint ?? Version);

        protected override void HandleRelatedContentChildren(ICollection<IContentSpec<Content>> x) {}
    }
}