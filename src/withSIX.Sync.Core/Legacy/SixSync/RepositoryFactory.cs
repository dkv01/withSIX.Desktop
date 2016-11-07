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
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Logging;
using withSIX.Sync.Core.Legacy.Status;
using withSIX.Sync.Core.Transfer;

namespace withSIX.Sync.Core.Legacy.SixSync
{
    public class RepositoryFactory : IEnableLogging
    {
        readonly ZsyncMake _zsyncMake;

        public RepositoryFactory(ZsyncMake zsyncMake) {
            _zsyncMake = zsyncMake;
        }

        internal Repository Init(IAbsoluteDirectoryPath folder, IReadOnlyCollection<Uri> hosts, SyncOptions opts) {
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

            var config = new RepoConfig { Hosts = hosts.ToList() };

            config.PackPath = opts.PackPath?.ToString();

            if (opts.Include != null)
                config.Include = opts.Include;

            if (opts.Exclude != null)
                config.Include = opts.Exclude;

            var guid = opts.RequiredGuid ?? Guid.NewGuid().ToString();

            var packVersion = new RepoVersion { Guid = guid };
            if (opts.ArchiveFormat != null)
                packVersion.ArchiveFormat = (string)opts.ArchiveFormat;

            var wdVersion = SyncEvilGlobal.Yaml.NewFromYaml<RepoVersion>(packVersion.ToYaml());

            SyncEvilGlobal.Yaml.ToYamlFile(config, configFile);
            SyncEvilGlobal.Yaml.ToYamlFile(packVersion, packVersionFile);
            SyncEvilGlobal.Yaml.ToYamlFile(wdVersion, wdVersionFile);

            return TryGetRepository(folder, opts, rsyncFolder);
        }

        public Repository Init(IAbsoluteDirectoryPath folder, IReadOnlyCollection<Uri> hosts,
            Action<SyncOptions> cfg = null) {
            Contract.Requires<ArgumentNullException>(folder != null);
            Contract.Requires<ArgumentNullException>(hosts != null);

            var opts = new SyncOptions();
            cfg?.Invoke(opts);

            return Init(folder, hosts, opts);
        }

        private static IAbsoluteDirectoryPath GetPackFolder(SyncOptions opts,
            IAbsoluteDirectoryPath rsyncFolder) => opts.PackPath ?? rsyncFolder.GetChildDirectoryWithName(Repository.PackFolderName);

        Repository TryGetRepository(IAbsoluteDirectoryPath folder, SyncOptions opts,
            IAbsoluteDirectoryPath rsyncFolder) {
            try {
                var repo = GetRepository(folder, opts);

                if (opts.MaxThreads.HasValue)
                    repo.MultiThreadingSettings.MaxThreads = opts.MaxThreads.Value;

                if (opts.RequiredVersion.HasValue)
                    repo.RequiredVersion = opts.RequiredVersion;

                if (opts.RequiredGuid != null)
                    repo.RequiredGuid = opts.RequiredGuid;

                if (opts.Output != null)
                    repo.Output = opts.Output;

                repo.LoadHosts();
                return repo;
            } catch (Exception) {
                Tools.FileUtil.Ops.DeleteWithRetry(rsyncFolder.ToString());
                throw;
            }
        }

        Repository GetRepository(IAbsoluteDirectoryPath folder, SyncOptions opts)
            => opts.Status != null
                ? new Repository(_zsyncMake, opts.Status, folder.ToString())
                : new Repository(_zsyncMake, folder.ToString());

        public async Task<Repository> Clone(IReadOnlyCollection<Uri> hosts, string folder, Action<SyncOptions> config = null) {
            Contract.Requires<ArgumentNullException>(folder != null);
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(folder));
            Contract.Requires<ArgumentNullException>(hosts != null);

            var opts = new SyncOptions();
            config?.Invoke(opts);
            if (opts.Path != null)
                folder = opts.Path.GetChildDirectoryWithName(folder).ToString();
            var repo = Init(folder.ToAbsoluteDirectoryPath(), hosts, opts);
            await repo.Update(opts).ConfigureAwait(false);
            return repo;
        }

        public Repository OpenOrInit(IAbsoluteDirectoryPath folder, Action<SyncOptions> config = null)
            => folder.GetChildDirectoryWithName(".rsync").Exists
                ? Open(folder, config)
                : Init(folder, new Uri[0], config);

        public Repository Open(IAbsoluteDirectoryPath folder, Action<SyncOptions> config = null) {
            Contract.Requires<ArgumentNullException>(folder != null);

            var opts = new SyncOptions();
            config?.Invoke(opts);

            var repo = GetRepository(folder, opts);
            if (opts.Output != null)
                repo.Output = opts.Output;

            return repo;
        }

        public Repository Open(string folder, Action<SyncOptions> config = null) {
            Contract.Requires<ArgumentNullException>(folder != null);
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(folder));
            return Open(folder.ToAbsoluteDirectoryPath(), config);
        }

        public Repository Convert(IAbsoluteDirectoryPath folder, Action<SyncOptions> config = null) {
            Contract.Requires<ArgumentNullException>(folder != null);

            var opts = new SyncOptions();
            config?.Invoke(opts);

            var hosts = opts.Hosts;
            var repo = Init(folder, hosts, opts);
            repo.Commit(false, false);
            return repo;
        }
    }


    public class SyncOptions
    {
        public List<Uri> Hosts { get; set; } = new List<Uri>();
        public List<string> Include { get; set; } = new List<string>();
        public List<string> Exclude { get; set; } = new List<string>();
        public IAbsoluteDirectoryPath PackPath { get; set; }
        public StatusRepo Status { get; set; }
        public string Output { get; set; }
        public IAbsoluteDirectoryPath Path { get; set; }
        public string RequiredGuid { get; set; }
        public string ArchiveFormat { get; set; }
        public int? MaxThreads { get; set; }
        public long? RequiredVersion { get; set; }
        public bool LocalOnly { get; set; }
        public bool AllowFullTransferFallBack { get; set; }
        public bool KeepCompressedFiles { get; set; }
    }
}