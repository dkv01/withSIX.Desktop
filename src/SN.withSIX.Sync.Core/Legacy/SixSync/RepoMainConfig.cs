// <copyright company="SIX Networks GmbH" file="RepoMainConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;

namespace SN.withSIX.Sync.Core.Legacy.SixSync
{
    public class RepoMainConfig : IBaseYaml
    {
        /*
:key: C:/users/patrick/documents/keys/openssh/id_rsa
:default_host: rsync@dev-heaven.net:/var/scm/rsync/rel
:default_host_path: .pack
         */
        public string Key { get; set; }
        public string DefaultHost { get; set; }
        public string DefaultHostPath { get; set; }
        public bool OverrideDowncase { get; set; }
        public bool SecureSsh { get; set; }

        public string ToYaml() {
            var graph = new Dictionary<string, object> {
                {":key", Key},
                {":default_host", DefaultHost},
                {":default_host_path", DefaultHostPath},
                {":secure_ssh", SecureSsh},
                {":override_downcase", OverrideDowncase}
            };
            return SyncEvilGlobal.Yaml.ToYaml(graph);
        }
    }
}