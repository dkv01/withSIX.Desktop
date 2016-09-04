// <copyright company="SIX Networks GmbH" file="ModNetworkContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Core.Games
{
    [DataContract]
    public class ModNetworkContent : NetworkContent, IModContent
    {
        protected ModNetworkContent() {}
        public ModNetworkContent(string packageName, Guid gameId) : base(packageName, gameId) {}
        public override string ContentSlug { get; } = "mods";

        public override async Task PostInstall(IInstallerSession installerSession, CancellationToken cancelToken,
            bool processed) {
            await base.PostInstall(installerSession, cancelToken, processed).ConfigureAwait(false);
            installerSession.RunCE(this);
        }

        public static ModNetworkContent FromSteamId(ulong contentId, Guid gameId) {
            var contentIdStr = contentId.ToString();
            var content = new ModNetworkContent(contentIdStr, gameId) {
                Id = contentId.CreateSteamContentIdGuid()
            };
            content.Publishers.Add(new ContentPublisher(Publisher.Steam, contentIdStr));
            return content;
        }

        public bool IsSteam() => Publishers.Count == 1 &&
                                 Publishers.Any(x => x.Publisher == Publisher.Steam);

        public void HandleOriginalGame(Guid originalGameId, string originalGameSlug, Guid gameId) {
            OriginalGameId = originalGameId;
            OriginalGameSlug = originalGameSlug;
            GameId = gameId;
        }
    }

    [DataContract]
    public class ModNetworkGroupContent : ModNetworkContent
    {
        protected ModNetworkGroupContent() {}

        public ModNetworkGroupContent(Guid id, string packageName, Guid gameId)
            : base(packageName, gameId) {
            Id = id;
        }
    }
}