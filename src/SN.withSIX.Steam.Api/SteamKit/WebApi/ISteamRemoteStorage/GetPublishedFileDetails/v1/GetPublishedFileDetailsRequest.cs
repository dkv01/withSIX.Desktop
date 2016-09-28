// <copyright company="SIX Networks GmbH" file="GetPublishedFileDetailsRequest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;

namespace SN.withSIX.Steam.Api.SteamKit.WebApi.ISteamRemoteStorage.GetPublishedFileDetails.v1
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class GetPublishedFileDetailsRequest : ApiRequestBase
    {
        public GetPublishedFileDetailsRequest(ulong fileid) {
            publishedfileids = new[] {fileid};
            itemcount = 1;
        }

        public ulong[] publishedfileids { get; set; }
        public int itemcount { get; set; }
        protected override Uri ApiUri
            => new Uri("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/");
    }
}