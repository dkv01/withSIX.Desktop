// <copyright company="SIX Networks GmbH" file="Request.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;
using SN.withSIX.Steam.Api.SteamKit.Utils;

namespace SN.withSIX.Steam.Api.SteamKit.WebApi.IPublishedFileService.QueryFiles.v1
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class Request : ApiAuthRequestBase
    {
        int? _numperpage;
        int? _page;

        public Request(string apiKey, int appId)
            : base(apiKey) {
            appid = appId;
        }

        public int appid { get; set; }
        public EPublishedFileQueryType query_type { get; set; }
        public int? page
        {
            get { return _page; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "Page is a One-Based index.");
                _page = value;
            }
        }
        public int? numperpage
        {
            get { return _numperpage; }
            set
            {
                if (value > 100) {
                    throw new ArgumentOutOfRangeException("value",
                        "Maximum results that can be returned in on query is 100");
                }
                _numperpage = value;
            }
        }
        public int? creator_appid { get; set; }
        public string[] requiredtags { get; set; }
        public string[] excludedtags { get; set; }
        public bool? match_all_tags { get; set; }
        public string[] required_flags { get; set; }
        public string[] omitted_flags { get; set; }
        public string search_text { get; set; }
        public int? filetype { get; set; }
        public long? child_publishedfileid { get; set; }
        public int? days { get; set; }
        public bool? include_recent_votes_only { get; set; }
        public int? cache_max_age_seconds { get; set; }
        public bool? totalonly { get; set; }
        public bool? return_vote_data { get; set; }
        public bool? return_tags { get; set; }
        public bool? return_kv_tags { get; set; }
        public bool? return_previews { get; set; }
        public bool? return_children { get; set; }
        public bool? return_short_description { get; set; }
        public bool? return_for_sale_data { get; set; }
        public bool? return_metadata { get; set; }
        protected override Uri ApiUri => new Uri("https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/");
    }
}