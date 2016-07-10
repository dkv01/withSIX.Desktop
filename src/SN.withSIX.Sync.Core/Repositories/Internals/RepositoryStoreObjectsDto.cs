// <copyright company="SIX Networks GmbH" file="RepositoryStoreObjectsDto.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;


namespace SN.withSIX.Sync.Core.Repositories.Internals
{
    [DataContract]
    public class RepositoryStoreObjectsDto
    {
        public RepositoryStoreObjectsDto() {
            Objects = new Dictionary<string, string>();
        }

        [DataMember, JsonProperty("objects")]
        public Dictionary<string, string> Objects { get; set; }
    }
}