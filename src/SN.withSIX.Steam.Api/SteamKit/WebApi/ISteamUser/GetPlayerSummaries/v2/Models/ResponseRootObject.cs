// <copyright company="SIX Networks GmbH" file="ResponseRootObject.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;

namespace SN.withSIX.Steam.Api.SteamKit.WebApi.ISteamUser.GetPlayerSummaries.v2.Models
{
    public sealed class ResponseRootObject
    {
        public Response response { get; set; }
    }

    public sealed class Response
    {
        public List<Player> players { get; set; }
    }
}