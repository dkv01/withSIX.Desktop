// <copyright company="SIX Networks GmbH" file="RepositoryRemote.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Sync.Core.Legacy;
using SN.withSIX.Sync.Core.Legacy.Status;
using SN.withSIX.Sync.Core.Transfer;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Sync.Core.Repositories.Internals
{
    public class RepositoryRemote
    {
        static readonly FileFetchInfo[] defaultFiles = {
            new FileFetchInfo(Repository.ConfigFile) {OnVerify = Repository.ConfirmValidity<RepositoryConfigDto>},
            new FileFetchInfo(Repository.PackageIndexFile) {
                OnVerify = Repository.ConfirmValidity<RepositoryStorePackagesDto>
            },
            new FileFetchInfo(Repository.CollectionIndexFile) {
                OnVerify = Repository.ConfirmValidity<RepositoryStoreBundlesDto>
            }
        };

        public RepositoryRemote() {
            Config = new RepositoryConfig();
            Index = new RepositoryStore();
        }

        public RepositoryRemote(Uri[] uri) : this() {
            Urls = uri;
        }

        public RepositoryRemote(Uri[] uri, Guid id) : this(uri) {
            Config.Uuid = id;
        }

        public RepositoryRemote(Uri uri)
            : this() {
            Urls = new[] {uri};
        }

        public RepositoryRemote(Uri uri, Guid id)
            : this(uri) {
            Config.Uuid = id;
        }

        public RepositoryConfig Config { get; private set; }
        Uri[] Urls { get; }
        public RepositoryStore Index { get; }
        public IAbsoluteDirectoryPath Path { get; set; }

        public IEnumerable<Uri> GetRemotes() {
            if ((Config.Remotes == null) || !Config.Remotes.Any())
                return Urls.Distinct();
            lock (Config.Remotes) {
                return Config.Remotes.Values
                    .SelectMany(x => x).Distinct();
            }
        }

        public Task LoadAsync(bool includeObjects = false, CancellationToken token = default(CancellationToken))
            => Task.Run(() => Load(includeObjects));

        public void Load(bool includeObjects = false) {
            LoadConfig();
            LoadPackages();
            LoadBundles();
            if (includeObjects)
                LoadObjects();
        }

        void LoadConfig() {
            Path.MakeSurePathExists();
            Config =
                Repository.TryLoad<RepositoryConfigDto, RepositoryConfig>(
                    Path.GetChildFileWithName(Repository.ConfigFile));
        }

        void LoadPackages() {
            Path.MakeSurePathExists();
            var index =
                Repository.TryLoad<RepositoryStorePackagesDto>(Path.GetChildFileWithName(Repository.PackageIndexFile));
            Index.Packages = index.Packages.ToDictionary(x => x.Key, x => x.Value);
            Index.PackagesContentTypes = index.PackagesContentTypes.ToDictionary(x => x.Key, x => x.Value);
        }

        void LoadBundles() {
            Path.MakeSurePathExists();
            Index.Bundles =
                Repository.TryLoad<RepositoryStoreBundlesDto>(Path.GetChildFileWithName(Repository.CollectionIndexFile))
                    .Bundles.ToDictionary(x => x.Key, x => x.Value);
        }

        void LoadObjects() {
            Path.MakeSurePathExists();
            Index.Objects =
                Repository.TryLoad<RepositoryStoreObjectsDto>(Path.GetChildFileWithName(Repository.ObjectIndexFile))
                    .Objects;
        }

        public async Task Update(CancellationToken token, bool inclObjects = false) {
            await
                DownloadFiles(inclObjects
                        ? defaultFiles.Concat(new[] {
                            new FileFetchInfo(Repository.ObjectIndexFile) {
                                OnVerify = Repository.ConfirmValidity<RepositoryStoreObjectsDto>
                            }
                        })
                        : defaultFiles, token)
                    .ConfigureAwait(false);

            await LoadAsync(inclObjects, token).ConfigureAwait(false);
        }

        public static int CalculateHttpFallbackAfter(int limit) => Math.Max((int) (limit/3.0), 1);

        async Task DownloadFiles(IEnumerable<FileFetchInfo> remoteFiles, CancellationToken token) {
            var retryLimit = 6;
            var statusRepo = new StatusRepo(token);
            await SyncEvilGlobal.DownloadHelper.DownloadFilesAsync(Urls, statusRepo,
                remoteFiles.ToDictionary(x => x,
                    x =>
                        (ITransferStatus)
                        new TransferStatus(x.FilePath) {
                            ZsyncHttpFallbackAfter = CalculateHttpFallbackAfter(retryLimit)
                        }), Path,
                retryLimit).ConfigureAwait(false);
        }
    }
}