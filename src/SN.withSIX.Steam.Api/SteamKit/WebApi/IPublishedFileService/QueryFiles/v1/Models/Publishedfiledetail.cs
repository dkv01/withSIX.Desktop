// <copyright company="SIX Networks GmbH" file="Publishedfiledetail.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SN.withSIX.Steam.Api.SteamKit.Utils;

namespace SN.withSIX.Steam.Api.SteamKit.WebApi.IPublishedFileService.QueryFiles.v1.Models
{
    public sealed class Publishedfiledetail
    {
        public EResult result { get; set; }
        public string publishedfileid { get; set; }
        public string creator { get; set; }
        public int creator_appid { get; set; }
        public int consumer_appid { get; set; }
        public int consumer_shortcutid { get; set; }
        public string filename { get; set; }
        public string file_size { get; set; }
        public string preview_file_size { get; set; }
        public Uri file_url { get; set; }
        public Uri preview_url { get; set; }
        public Uri url { get; set; }
        public string hcontent_file { get; set; }
        public string hcontent_preview { get; set; }
        public string title { get; set; }
        public string file_description { get; set; }
        [JsonConverter(typeof (UnixTimestampConverter))]
        public DateTime? time_created { get; set; }
        [JsonConverter(typeof (UnixTimestampConverter))]
        public DateTime? time_updated { get; set; }
        public int visibility { get; set; }
        public int flags { get; set; }
        public bool workshop_file { get; set; }
        public bool workshop_accepted { get; set; }
        public bool show_subscribe_all { get; set; }
        public int num_comments_developer { get; set; }
        public int num_comments_public { get; set; }
        public bool banned { get; set; }
        public string ban_reason { get; set; }
        public string banner { get; set; }
        public bool can_be_deleted { get; set; }
        public bool incompatible { get; set; }
        public string app_name { get; set; }
        public int file_type { get; set; }
        public bool can_subscribe { get; set; }
        public int subscriptions { get; set; }
        public int favorited { get; set; }
        public int followers { get; set; }
        public int lifetime_subscriptions { get; set; }
        public int lifetime_favorited { get; set; }
        public int lifetime_followers { get; set; }
        public int views { get; set; }
        public bool spoiler_tag { get; set; }
        public int num_children { get; set; }
        public int num_reports { get; set; }
        public List<Tag> tags { get; set; }
    }
}