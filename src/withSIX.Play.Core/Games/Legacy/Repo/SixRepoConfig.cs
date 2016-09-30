// <copyright company="SIX Networks GmbH" file="SixRepoConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace withSIX.Play.Core.Games.Legacy.Repo
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Sync.Core.Models.Repositories.SixSync"
     )]
    public class SixRepoConfig : IBaseYaml
    {
        public SixRepoConfig() {
            Apps = new Dictionary<string, SixRepoApp>();
            Mods = new Dictionary<string, SixRepoMod>();
            Missions = new Dictionary<string, string>();
            MPMissions = new Dictionary<string, string>();
            Hosts = new Uri[0];
        }

        [DataMember]
        public Dictionary<string, SixRepoApp> Apps { get; private set; }
        [DataMember]
        public Dictionary<string, string> MPMissions { get; private set; }
        [DataMember]
        public Dictionary<string, string> Missions { get; private set; }
        [DataMember]
        public Dictionary<string, SixRepoMod> Mods { get; private set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Homepage { get; set; }
        [DataMember]
        public string Image { get; set; }
        [DataMember]
        public string Uuid { get; set; }
        [DataMember]
        public string ImageLarge { get; set; }
        [DataMember]
        public string ServerModsPath { get; set; }
        [DataMember]
        public string ArchiveFormat { get; set; }
        [DataMember]
        public Uri[] Hosts { get; set; }
        [DataMember]
        public string[] Servers { get; set; }
        [DataMember]
        public string[] Include { get; set; }
        [DataMember]
        public int? MaxThreads { get; set; }

        public string ToYaml() {
            var graph = new Dictionary<string, object> {
                {":name", Name},
                {":uuid", Uuid},
                {":homepage", Homepage},
                {":server_mods_path", ServerModsPath},
                {":archive_format", ArchiveFormat},
                {":max_threads", MaxThreads},
                {":hosts", Hosts},
                {":image", Image},
                {":image_large", ImageLarge},
                {":servers", Servers},
                {":missions", Missions},
                {":mpmissions", MPMissions},
                {":mods", Mods},
                {":apps", Apps}
            };
            return SyncEvilGlobal.Yaml.ToYaml(graph);
        }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context) {
            if (Apps == null)
                Apps = new Dictionary<string, SixRepoApp>();
            if (Mods == null)
                Mods = new Dictionary<string, SixRepoMod>();
            if (Missions == null)
                Missions = new Dictionary<string, string>();
            if (MPMissions == null)
                MPMissions = new Dictionary<string, string>();
            if (Hosts == null)
                Hosts = new Uri[0];
        }
    }



    public class InvalidHostFoundException : Exception
    {
        public InvalidHostFoundException(string message) : base(message) {}
    }
}