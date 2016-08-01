// <copyright company="SIX Networks GmbH" file="GetPlayerSummariesRequest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;

namespace SN.withSIX.Steam.Core.SteamKit.WebApi.ISteamUser.GetPlayerSummaries.v2
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class GetPlayerSummariesRequest : ApiAuthRequestBase
    {
        public GetPlayerSummariesRequest(string apiKey, ulong playerid)
            : base(apiKey) {
            steamids = playerid;
        }

        public ulong steamids { get; set; }
        protected override Uri ApiUri => new Uri("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/");
    }
}