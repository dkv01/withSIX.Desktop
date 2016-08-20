// <copyright company="SIX Networks GmbH" file="ModDto.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.v3;

namespace SN.withSIX.Mini.Infra.Api.WebApi
{
    public class ModDtoV2 : ContentDtoV2
    {
        //public string CppName { get; set; }
        public string LatestStableVersion { get; set; }
        public string GetVersion() => LatestStableVersion ?? Version;
    }

    public class ModDtoV2WithPubs : ModDtoV2
    {
        // We want to ignore these for original V2 API for now..
        public List<ContentPublisherApiJson> Publishers { get; set; }
    }

    public class ModClientApiJsonV3WithGameId : ModClientApiJson
    {
        public string GetVersion() => LatestStableVersion ?? Version;
        private Guid _gameId;
        public Guid GameId
        {
            get { return _gameId; }
            set
            {
                if (value == Guid.Empty) throw new ArgumentException(nameof(value));
                _gameId = value;
            }
        }
        // Only used for BWC
        public List<string> Tags { get; set; }
    }
}