// <copyright company="SIX Networks GmbH" file="RepoVersion.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;

namespace SN.withSIX.Sync.Core.Legacy.SixSync
{
    public class RepoVersion : IBaseYaml
    {
        public Dictionary<string, string> Pack = new Dictionary<string, string>();
        public Dictionary<string, string> WD = new Dictionary<string, string>();

        public RepoVersion() {
            ArchiveFormat = Repository.DefaultArchiveFormat;
            FormatVersion = 1;
            Pack = new Dictionary<string, string>();
            WD = new Dictionary<string, string>();
        }

        public string ArchiveFormat { get; set; }
        public long Version { get; set; }
        public long FormatVersion { get; set; }
        public string Guid { get; set; }
        public long PackSize { get; set; }
        public long WdSize { get; set; }

        public string ToYaml() {
            var graph = new Dictionary<string, object> {
                {":archive_format", ArchiveFormat},
                {":format_version", FormatVersion},
                {":guid", Guid},
                {":version", Version},
                {":pack_size", PackSize},
                {":wd_size", WdSize},
                {":pack", Pack},
                {":wd", WD}
            };
            return SyncEvilGlobal.Yaml.ToYaml(graph);
        }

        public string PrettyPrint() => string.Format(
            "Guid: {6}\nVersion: {7}\nArchiveFormat: {0}\nFormatVersion: {1}\nPackSize: {2}\nWdSize: {3}\nPack: {4}\nWD: {5}",
            ArchiveFormat, FormatVersion, PackSize, WdSize, string.Join(", ", Pack),
            string.Join(", ", WD), Guid, Version);
    }
}