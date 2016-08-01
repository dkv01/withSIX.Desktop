// <copyright company="SIX Networks GmbH" file="Player.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using Newtonsoft.Json;
using SN.withSIX.Steam.Core.SteamKit.Utils;

namespace SN.withSIX.Steam.Core.SteamKit.WebApi.ISteamUser.GetPlayerSummaries.v2.Models
{
    public sealed class Player
    {
        public string steamid { get; set; }
        public uint communityvisibilitystate { get; set; }
        public uint profilestate { get; set; }
        public string personaname { get; set; }
        [JsonConverter(typeof (UnixTimestampConverter))]
        public DateTime lastlogoff { get; set; }
        public string profileurl { get; set; }
        public string avatar { get; set; }
        public string avatarmedium { get; set; }
        public string avatarfull { get; set; }
        public uint personastate { get; set; }
        public string realname { get; set; }
        public string primaryclanid { get; set; }
        [JsonConverter(typeof (UnixTimestampConverter))]
        public DateTime timecreated { get; set; }
        public uint personastateflags { get; set; }
        public string gameid { get; set; }
    }
}