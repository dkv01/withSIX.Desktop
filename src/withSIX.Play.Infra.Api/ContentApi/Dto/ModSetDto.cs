// <copyright company="SIX Networks GmbH" file="ModSetDto.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using withSIX.Api.Models.Content.v2;

namespace withSIX.Play.Infra.Api.ContentApi.Dto
{

    class MissionDto : MissionClientApiJson, ISyncBaseDto {}

    [DataContract]
    class ModSetDto : ContentBaseDto
    {
        [DataMember]
        [JsonProperty("image")]
        public string Image { get; set; }
        [DataMember]
        [JsonProperty("image_large")]
        public string ImageLarge { get; set; }
        [DataMember]
        [JsonProperty("parent")]
        public string Parent { get; set; }
        [DataMember]
        [JsonProperty("mods")]
        public List<string> Mods { get; set; }
        [DataMember]
        [JsonProperty("changelog_url")]
        public string ChangelogUrl { get; set; }
        [DataMember]
        [JsonProperty("support_url")]
        public string SupportUrl { get; set; }
        [DataMember]
        [JsonProperty("game_uuid")]
        public Guid GameUuid { get; set; }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context) {
            if (Uuid != null) {
                Guid id;
                if (Guid.TryParse(Uuid, out id))
                    Id = id;
                else
                    Id = Guid.NewGuid(); // tsk
                Uuid = null;
            }
        }
    }
}