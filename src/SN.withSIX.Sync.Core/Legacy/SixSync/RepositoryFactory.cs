// <copyright company="SIX Networks GmbH" file="RepositoryFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Logging;
using SN.withSIX.Sync.Core.Legacy.Status;
using SN.withSIX.Sync.Core.Transfer;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Sync.Core.Legacy.SixSync
{
    public class RepositoryFactory : IEnableLogging
    {
        readonly ZsyncMake _zsyncMake;

        public RepositoryFactory(ZsyncMake zsyncMake) {
            _zsyncMake = zsyncMake;
        }

        public Repository Init(IAbsoluteDirectoryPath folder, IReadOnlyCollection<Uri> hosts,
            Dictionary<string, object> opts = null) {
            Contract.Requires<ArgumentNullException>(folder != null);
            Contract.Requires<ArgumentNullException>(hosts != null);

            if (opts == null)
                opts = new Dictionary<string, object>();

            var rsyncFolder = folder.GetChildDirectoryWithName(Repository.RepoFolderName);
            if (rsyncFolder.Exists)
                throw new Exception("Already appears to be a repository");

            var packFolder = GetPackFolder(opts, rsyncFolder);
            var configFile = rsyncFolder.GetChildFileWithName(Repository.ConfigFileName);
            var wdVersionFile = rsyncFolder.GetChildFileWithName(Repository.VersionFileName);
            var packVersionFile = packFolder.GetChildFileWithName(Repository.VersionFileName);

            this.Logger().Info("Initializing {0}", folder);
            rsyncFolder.MakeSurePathExists();
            packFolder.MakeSurePathExists();

            var config = new RepoConfig {Hosts = hosts.ToArray()};

            if (opts.ContainsKey("pack_path"))
                config.PackPath = (string) opts["pack_path"];

            if (opts.ContainsKey("include"))
                config.Include = (string[]) opts["include"];

            if (opts.ContainsKey("exclude"))
                config.Exclude = (string[]) opts["exclude"];

            var guid = opts.ContainsKey("required_guid")
                ? (string) opts["required_guid"]
                : Guid.NewGuid().ToString();

            var packVersion = new RepoVersion {Guid = guid};
            if (opts.ContainsKey("archive_format"))
                packVersion.ArchiveFormat = (string) opts["archive_format"];

            var wdVersion = SyncEvilGlobal.Yaml.NewFromYaml<RepoVersion>(packVersion.ToYaml());

            SyncEvilGlobal.Yaml.ToYamlFile(config, configFile);
            SyncEvilGlobal.Yaml.ToYamlFile(packVersion, packVersionFile);
            SyncEvilGlobal.Yaml.ToYamlFile(wdVersion, wdVersionFile);

            return TryGetRepository(folder, opts, rsyncFolder);
        }

        private static IAbsoluteDirectoryPath GetPackFolder(IReadOnlyDictionary<string, object> opts,
            IAbsoluteDirectoryPath rsyncFolder) => opts.ContainsKey("pack_path")
                ? ((string) opts["pack_path"]).ToAbsoluteDirectoryPath()
                : rsyncFolder.GetChildDirectoryWithName(Repository.PackFolderName);

        Repository TryGetRepository(IAbsoluteDirectoryPath folder, Dictionary<string, object> opts,
            IAbsoluteDirectoryPath rsyncFolder) {
            try {
                var repo = GetRepository(folder, opts);

                if (opts.ContainsKey("max_threads"))
                    repo.MultiThreadingSettings.MaxThreads = (int) opts["max_threads"];

                if (opts.ContainsKey("required_version"))
                    repo.RequiredVersion = (long?) opts["required_version"];

                if (opts.ContainsKey("required_guid"))
                    repo.RequiredGuid = (string) opts["required_guid"];

                if (opts.ContainsKey("output"))
                    repo.Output = (string) opts["output"];

                repo.LoadHosts();
                return repo;
            } catch (Exception) {
                Tools.FileUtil.Ops.DeleteWithRetry(rsyncFolder.ToString());
                throw;
            }
        }

        Repository GetRepository(IAbsoluteDirectoryPath folder, IDictionary<string, object> opts)
            => opts.ContainsKey("status")
                ? new Repository(_zsyncMake, (StatusRepo) opts["status"], folder.ToString())
                : new Repository(_zsyncMake, folder.ToString());

        public async Task<Repository> Clone(Uri[] hosts, string folder, Dictionary<string, object> opts = null) {
            Contract.Requires<ArgumentNullException>(folder != null);
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(folder));
            Contract.Requires<ArgumentNullException>(hosts != null);

            if (opts == null)
                opts = new Dictionary<string, object>();
            if (opts.ContainsKey("path"))
                folder = Path.Combine((string) opts["path"], folder);
            var repo = Init(folder.ToAbsoluteDirectoryPath(), hosts, opts);
            await repo.Update(opts).ConfigureAwait(false);
            return repo;
        }

        public Repository OpenOrInit(IAbsoluteDirectoryPath folder, Dictionary<string, object> opts = null)
            => folder.GetChildDirectoryWithName(".rsync").Exists
                ? Open(folder, opts)
                : Init(folder, new Uri[0], opts);

        public Repository Open(IAbsoluteDirectoryPath folder, Dictionary<string, object> opts = null) {
            Contract.Requires<ArgumentNullException>(folder != null);

            if (opts == null)
                opts = new Dictionary<string, object>();

            var repo = GetRepository(folder, opts);
            if (opts.ContainsKey("output"))
                repo.Output = (string) opts["output"];

            return repo;
        }

        public Repository Open(string folder, Dictionary<string, object> opts = null) {
            Contract.Requires<ArgumentNullException>(folder != null);
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(folder));
            return Open(folder.ToAbsoluteDirectoryPath(), opts);
        }

        public Repository Convert(string folder, Dictionary<string, object> opts = null) {
            Contract.Requires<ArgumentNullException>(folder != null);
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(folder));

            if (opts == null)
                opts = new Dictionary<string, object>();

            var hosts = opts.ContainsKey("hosts") && opts["hosts"] != null ? (Uri[]) opts["hosts"] : new Uri[0];
            var repo = Init(folder.ToAbsoluteDirectoryPath(), hosts, opts);
            repo.Commit(false, false);
            return repo;
        }
    }
}