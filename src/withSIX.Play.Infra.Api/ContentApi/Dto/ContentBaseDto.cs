// <copyright company="SIX Networks GmbH" file="ContentBaseDto.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

using withSIX.Core.Extensions;
using withSIX.Api.Models.Extensions;

namespace withSIX.Play.Infra.Api.ContentApi.Dto
{
    [DataContract]
    class NewSyncBaseDto : ISyncBaseDto
    {
        [DataMember]
        public Guid Id { get; set; }
        [DataMember, JsonConverter(typeof (LockedGlobalizationDateConverter))]
        public DateTime CreatedAt { get; set; }
        [DataMember, JsonConverter(typeof (LockedGlobalizationDateConverter))]
        public DateTime UpdatedAt { get; set; }
    }

    [DataContract]
    class NewContentBaseDto : NewSyncBaseDto
    {
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string Author { get; set; }
        [DataMember]
        public string HomepageUrl { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Version { get; set; }
    }

    [DataContract]
    class ContentBaseDto : SyncBaseDto
    {
        [DataMember]
        [JsonProperty("description")]
        public string Description { get; set; }
        [DataMember]
        [JsonProperty("author")]
        public string Author { get; set; }
        [DataMember]
        [JsonProperty("homepage_url")]
        public string HomepageUrl { get; set; }
        [DataMember]
        [JsonProperty("name")]
        public string Name { get; set; }
        [DataMember]
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}