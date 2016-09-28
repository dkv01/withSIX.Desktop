// <copyright company="SIX Networks GmbH" file="CustomRepo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Sync.Core.Legacy.SixSync.CustomRepo.dtos;
using SN.withSIX.Sync.Core.Legacy.Status;
using SN.withSIX.Sync.Core.Transfer;

namespace SN.withSIX.Sync.Core.Legacy.SixSync.CustomRepo
{
    public class CustomRepo
    {
        public CustomRepo(Uri uri) {
            Uri = uri;
        }

        public Uri Uri { get; private set; }

        protected virtual Dictionary<string, SixRepoModDto> Mods { get; set; } = new Dictionary<string, SixRepoModDto>()
            ;
        protected virtual ICollection<Uri> Hosts { get; private set; } = new List<Uri>();

        public bool Loaded { get; set; }

        public static Uri GetRepoUri(Uri r) {
            var url = r.ToString();
            return !url.EndsWith("config.yml")
                ? new Uri(url.Substring(0, url.Length - Path.GetFileName(r.AbsolutePath).Length) + "config.yml")
                : r;
        }

        public async Task Load(IStringDownloader downloader, Uri uri) {
            Uri = uri;
            var config = await SyncEvilGlobal.Yaml.GetYaml<SixRepoConfigDto>(uri).ConfigureAwait(false);
            Mods = config.Mods;
            Hosts = config.Hosts.ToList();
            Loaded = true;
        }

        public bool ExistsAndIsRightVersion(string name, IAbsoluteDirectoryPath destination) {
            var mod = GetMod(name);
            var folder = destination.GetChildDirectoryWithName(mod.Key);
            var rsyncDir = folder.GetChildDirectoryWithName(Repository.RepoFolderName);
            return rsyncDir.Exists && IsRightVersion(rsyncDir, mod);
        }

        // TODO: localOnly if no update available? - so for local diagnose etc..
        public async Task GetMod(string name, IAbsoluteDirectoryPath destination, IAbsoluteDirectoryPath packPath,
            StatusRepo status, bool force = false) {
            var mod = GetMod(name);
            var folder = destination.GetChildDirectoryWithName(mod.Key);

            var opts = GetOpts(packPath, status, mod);
            if (!folder.Exists) {
                await
                    Repository.Factory.Clone((Uri[]) opts["hosts"], folder.ToString(), opts)
                        .ConfigureAwait(false);
                return;
            }

            var rsyncDir = folder.GetChildDirectoryWithName(Repository.RepoFolderName);
            if (!force && rsyncDir.Exists && IsRightVersion(rsyncDir, mod))
                return;

            var repo = GetRepo(rsyncDir, folder, opts);
            await repo.Update(opts).ConfigureAwait(false);
        }

        public KeyValuePair<string, SixRepoModDto> GetMod(string name)
            => Mods.First(x => x.Key.Equals(name, StringComparison.CurrentCultureIgnoreCase));

        bool IsRightVersion(IAbsoluteDirectoryPath rsyncDir, KeyValuePair<string, SixRepoModDto> mod) {
            var versionFile = rsyncDir.GetChildFileWithName(Repository.VersionFileName);
            if (!versionFile.Exists)
                return false;

            var repoInfo = TryReadRepoFile(versionFile);
            return (repoInfo != null) && (repoInfo.Guid == mod.Value.Guid) && (repoInfo.Version == mod.Value.Version);
        }

        static Repository GetRepo(IAbsoluteDirectoryPath rsyncDir,
            IAbsoluteDirectoryPath folder, Dictionary<string, object> opts) => rsyncDir.Exists
            ? Repository.Factory.Open(folder.ToString(), opts)
            : Repository.Factory.Convert(folder.ToString(), opts);

        // pff, better use a real param object!
        Dictionary<string, object> GetOpts(IAbsoluteDirectoryPath packPath, StatusRepo status,
            KeyValuePair<string, SixRepoModDto> mod) => new Dictionary<string, object> {
            {"hosts", Hosts.Select(x => new Uri(x, mod.Key)).ToArray()},
            {"required_version", mod.Value.Version},
            {"required_guid", mod.Value.Guid},
            {"pack_path", packPath.GetChildFileWithName(mod.Key).ToString()},
            {"status", status}
        };

        public bool HasMod(string name) => Mods.Keys.ContainsIgnoreCase(name);

        RepoVersion TryReadRepoFile(IAbsoluteFilePath path) {
            try {
                return SyncEvilGlobal.Yaml.NewFromYamlFile<RepoVersion>(path);
            } catch (YamlParseException e) {
                //this.Logger().FormattedWarnException(e, _mod.Name);
                return new RepoVersion();
            } catch (YamlExpectedOtherNodeTypeException e) {
                //this.Logger().FormattedWarnException(e, _mod.Name);
                return new RepoVersion();
            }
        }
    }
}