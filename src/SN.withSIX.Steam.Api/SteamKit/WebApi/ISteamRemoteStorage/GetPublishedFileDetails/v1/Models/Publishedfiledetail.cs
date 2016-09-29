// <copyright company="SIX Networks GmbH" file="Publishedfiledetail.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using withSIX.Steam.Api.SteamKit.Utils;

namespace withSIX.Steam.Api.SteamKit.WebApi.ISteamRemoteStorage.GetPublishedFileDetails.v1.Models
{
    public sealed class Publishedfiledetail
    {
        public string publishedfileid { get; set; }
        public EResult result { get; set; }
        public string creator { get; set; }
        public uint creator_app_id { get; set; }
        public uint consumer_app_id { get; set; }
        public string filename { get; set; }
        public long file_size { get; set; }
        public Uri file_url { get; set; }
        public string hcontent_file { get; set; }
        public Uri preview_url { get; set; }
        public string hcontent_preview { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime? time_created { get; set; }
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime? time_updated { get; set; }
        public uint visibility { get; set; }
        public uint banned { get; set; }
        public string ban_reason { get; set; }
        public uint subscriptions { get; set; }
        public uint favorited { get; set; }
        public uint lifetime_subscriptions { get; set; }
        public uint lifetime_favorited { get; set; }
        public uint views { get; set; }
        public List<Tag> tags { get; set; }
    }
}