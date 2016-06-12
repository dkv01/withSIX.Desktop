// <copyright company="SIX Networks GmbH" file="ModNetworkContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace SN.withSIX.Mini.Core.Games
{
    [DataContract]
    public class ModNetworkContent : NetworkContent, IModContent
    {
        protected ModNetworkContent() {}
        public ModNetworkContent(string name, string packageName, Guid gameId) : base(name, packageName, gameId) {}
        public override string ContentSlug { get; } = "mods";

        public override async Task PostInstall(IInstallerSession installerSession, CancellationToken cancelToken,
            bool processed) {
            await base.PostInstall(installerSession, cancelToken, processed).ConfigureAwait(false);
            installerSession.RunCE(this);
        }
    }

    [DataContract]
    public class ModNetworkGroupContent : ModNetworkContent
    {
        protected ModNetworkGroupContent() {}

        public ModNetworkGroupContent(Guid id, string name, string packageName, Guid gameId)
            : base(name, packageName, gameId) {
            Id = id;
        }
    }
}