// <copyright company="SIX Networks GmbH" file="LocalContent.cs">
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
    public class BasicInstallInfo
    {
        public string Version { get; set; }
        public long Size { get; set; }
        public long SizePacked { get; set; }
    }

    [DataContract]
    public abstract class LocalContent : Content, IUninstallableContent, IContentWithPackageName
    {
        private readonly Lazy<ContentPublisher> _source;

        protected LocalContent() {
            _source = SystemExtensions.CreateLazy(() => new ContentPublisher(Publisher.withSIX, PackageName));
        }

        protected LocalContent(string packageName, Guid gameId, string version) : base(gameId) {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(packageName));
            PackageName = packageName;
            Version = version;
            _source = SystemExtensions.CreateLazy(() => new ContentPublisher(Publisher.withSIX, PackageName));
        }

        protected LocalContent(string packageName, Guid gameId, BasicInstallInfo basicInstallInfo)
            : this(packageName, gameId, basicInstallInfo.Version) {
            Size = basicInstallInfo.Size;
            SizePacked = basicInstallInfo.SizePacked;
            Installed(basicInstallInfo.Version, true);
        }

        [DataMember]
        public string ContentSlug { get; protected set; }

        public ContentPublisher GetSource(IHaveSourcePaths game) => _source.Value;
        public void OverrideSource(Publisher publisher) {}

        [DataMember]
        public string PackageName { get; set; }

        public string GetFQN(string constraint = null) {
            var v = constraint ?? Version;
            if (v == null)
                return PackageName.ToLower();
            return PackageName.ToLower() + "-" + v;
        }

        public IAbsoluteDirectoryPath GetSourceDirectory(IHaveSourcePaths game)
            => game.ContentPaths.Path.GetChildDirectoryWithName(PackageName);

        public Task Uninstall(IUninstallSession installerSession, CancellationToken cancelToken,
            string constraint = null) => installerSession.Uninstall(this);

        protected override IContentSpec<Content> CreateRelatedSpec(string constraint) => new LocalContentSpec(this, constraint ?? Version);

        protected override void HandleRelatedContentChildren(ICollection<IContentSpec<Content>> x) {}
    }

    [DataContract]
    public class ModLocalContent : LocalContent, IModContent
    {
        protected ModLocalContent() {}

        public ModLocalContent(string packageName, Guid gameId, BasicInstallInfo basicInstallInfo)
            : base(packageName, gameId, basicInstallInfo) {
            ContentSlug = "mods";
        }

        public ModLocalContent(string packageName, Guid gameId, string version)
            : base(packageName, gameId, version) {}
    }

    [DataContract]
    public class ModRepoContent : ModLocalContent
    {
        protected ModRepoContent() {}

        public ModRepoContent(string packageName, Guid gameId, string version)
            : base(packageName, gameId, version) {}

        [DataMember]
        // TODO: Actually build dependencies out of objects instead of strings
        public List<string> Dependencies { get; set; } = new List<string>();

        protected override IContentSpec<Content> CreateRelatedSpec(string constraint) => new ModRepoContentSpec(this, constraint ?? Version);

        protected override void HandleRelatedContentChildren(ICollection<IContentSpec<Content>> x) {
            // TODO: Dependencies of dependencies
            x.AddRange(
                Dependencies.Select(d => new ModRepoContentSpec(new ModRepoContent(d.ToLower(), GameId, null))));
        }
    }

    [DataContract]
    public class MissionLocalContent : LocalContent, IMissionContent
    {
        protected MissionLocalContent() {}

        public MissionLocalContent(string name, string packageName, Guid gameId, string version)
            : base(packageName, gameId, version) {}

        public MissionLocalContent(string name, string packageName, Guid gameId, BasicInstallInfo basicInstallInfo)
            : base(packageName, gameId, basicInstallInfo) {
            ContentSlug = "missions";
        }
    }
}