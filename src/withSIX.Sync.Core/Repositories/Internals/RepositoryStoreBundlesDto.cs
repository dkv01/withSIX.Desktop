// <copyright company="SIX Networks GmbH" file="RepositoryStoreBundlesDto.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace withSIX.Sync.Core.Repositories.Internals
{
    [DataContract]
    public class RepositoryStoreBundlesDto
    {
        public RepositoryStoreBundlesDto() {
            Bundles = new Dictionary<string, List<string>>();
        }

        [DataMember, JsonProperty("bundles")]
        public Dictionary<string, List<string>> Bundles { get; set; }
    }
}