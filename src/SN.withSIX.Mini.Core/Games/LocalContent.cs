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

        protected LocalContent(string name, string packageName, Guid gameId, string version) : base(name, gameId) {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(packageName));
            PackageName = packageName;
            Version = version;
            _source = SystemExtensions.CreateLazy(() => new ContentPublisher(Publisher.withSIX, PackageName));
        }

        protected LocalContent(string name, string packageName, Guid gameId, BasicInstallInfo basicInstallInfo)
            : this(name, packageName, gameId, basicInstallInfo.Version) {
            Size = basicInstallInfo.Size;
            SizePacked = basicInstallInfo.SizePacked;
            Installed(basicInstallInfo.Version, true);
        }

        [DataMember]
        public string ContentSlug { get; protected set; }

        public ContentPublisher Source => _source.Value;
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

        public override IEnumerable<IContentSpec<Content>> GetRelatedContent(List<IContentSpec<Content>> list = null,
            string constraint = null) => HandleLocal(list, constraint);

        protected virtual IEnumerable<IContentSpec<Content>> HandleLocal(List<IContentSpec<Content>> list,
            string constraint) {
            if (list == null)
                list = new List<IContentSpec<Content>>();

            if (list.Select(x => x.Content).Contains(this))
                return list;

            var spec = new LocalContentSpec(this, constraint ?? Version);
            list.Add(spec);

            return list;
        }
    }

    [DataContract]
    public class ModLocalContent : LocalContent, IModContent
    {
        protected ModLocalContent() {}

        public ModLocalContent(string name, string packageName, Guid gameId, BasicInstallInfo basicInstallInfo)
            : base(name, packageName, gameId, basicInstallInfo) {
            ContentSlug = "mods";
        }

        public ModLocalContent(string name, string packageName, Guid gameId, string version)
            : base(name, packageName, gameId, version) {}
    }

    [DataContract]
    public class ModRepoContent : ModLocalContent
    {
        protected ModRepoContent() {}

        public ModRepoContent(string name, string packageName, Guid gameId, string version)
            : base(name, packageName, gameId, version) {}

        [DataMember]
        // TODO: Actually build dependencies out of objects instead of strings
        public List<string> Dependencies { get; set; } = new List<string>();

        protected override IEnumerable<IContentSpec<Content>> HandleLocal(List<IContentSpec<Content>> list,
            string constraint) {
            if (list == null)
                list = new List<IContentSpec<Content>>();

            if (list.Select(x => x.Content).Contains(this))
                return list;

            var spec = new ModRepoContentSpec(this, constraint ?? Version);
            list.Add(spec);
            // TODO: Dependencies of dependencies
            list.AddRange(
                Dependencies.Select(d => new ModRepoContentSpec(new ModRepoContent(d, d.ToLower(), GameId, null))));
            list.RemoveAll(x => x.Content == this);
            list.Add(spec);


            return list;
        }
    }

    [DataContract]
    public class MissionLocalContent : LocalContent, IMissionContent
    {
        protected MissionLocalContent() {}

        public MissionLocalContent(string name, string packageName, Guid gameId, string version)
            : base(name, packageName, gameId, version) {}

        public MissionLocalContent(string name, string packageName, Guid gameId, BasicInstallInfo basicInstallInfo)
            : base(name, packageName, gameId, basicInstallInfo) {
            ContentSlug = "missions";
        }
    }
}