// <copyright company="SIX Networks GmbH" file="RepoConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace withSIX.Sync.Core.Legacy.SixSync
{
    public class RepoConfig : IBaseYaml //, IYamlSerializable
    {
        public RepoConfig() {
            Hosts = new List<Uri>(0);
            Exclude = new List<string>();
            Include = new List<string>();
        }

        public string PackPath { get; set; }
        public List<Uri> Hosts { get; set; }
        public List<string> Exclude { get; set; }
        public List<string> Include { get; set; }

        public string ToYaml() {
            var graph = new Dictionary<string, object> {
                {":pack_path", PackPath},
                {":exclude", Exclude},
                {":include", Include},
                {":hosts", Hosts.Select(x => x.ToString())}
            };
            return SyncEvilGlobal.Yaml.ToYaml(graph);
        }

        public string PrettyPrint() =>
            $"PackPath: {PackPath}\nHosts: {string.Join(", ", Hosts.Select(x => x.ToString()))}";
    }
}