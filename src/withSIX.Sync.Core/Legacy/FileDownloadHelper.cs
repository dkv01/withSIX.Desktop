// <copyright company="SIX Networks GmbH" file="FileDownloadHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models.Extensions;
using withSIX.Sync.Core.Legacy.Status;
using withSIX.Sync.Core.Repositories.Internals;
using withSIX.Sync.Core.Transfer;
using withSIX.Sync.Core.Transfer.MirrorSelectors;
using withSIX.Sync.Core.Transfer.Specs;

namespace withSIX.Sync.Core.Legacy
{
    public interface IFileDownloadHelper
    {
        ExportLifetimeContext<IMirrorSelector> StartMirrorSession(int limit, IReadOnlyCollection<Uri> remotes);
        ExportLifetimeContext<IMirrorSelector> StartMirrorSession(IReadOnlyCollection<Uri> remotes);

        Task DownloadFileAsync(string remoteFile, IAbsoluteFilePath destinationPath, IMirrorSelector selector,
            ITransferStatus status, CancellationToken token);


        Task DownloadFilesAsync(IReadOnlyCollection<Uri> remotes, StatusRepo sr,
            IDictionary<FileFetchInfo, ITransferStatus> transferStatuses, IAbsoluteDirectoryPath destination);

        Task DownloadFileAsync(string remoteFile, IAbsoluteDirectoryPath destinationPath,
            IReadOnlyCollection<Uri> remotes,
            CancellationToken token, int limit);

        Task DownloadFilesAsync(IReadOnlyCollection<Uri> remotes, StatusRepo sr,
            IDictionary<FileFetchInfo, ITransferStatus> transferStatuses,
            IAbsoluteDirectoryPath destination,
            int limit);

        Task DownloadFileAsync(Uri uri, IAbsoluteFilePath localFile);

        Task DownloadFileAsync(string remoteFile, IAbsoluteDirectoryPath destinationPath,
            IReadOnlyCollection<Uri> remotes, CancellationToken token, int limit,
            Func<IAbsoluteFilePath, bool> confirmValidity);
    }

    public class FileDownloadHelper : IFileDownloadHelper
    {
        readonly Func<IReadOnlyCollection<Uri>, ExportLifetimeContext<IMirrorSelector>> _createMirrorSelector;
        readonly Func<int, IReadOnlyCollection<Uri>, ExportLifetimeContext<IMirrorSelector>>
            _createMirrorSelectorWithLimit;
        readonly Func<IMirrorSelector, ExportLifetimeContext<IMultiMirrorFileDownloader>>
            _createMultiMirrorFileDownloader;
        readonly Func<IMultiMirrorFileDownloader, ExportLifetimeContext<IFileQueueDownloader>>
            _createQueueDownloader;
        readonly IFileDownloader _downloader;

        public FileDownloadHelper(IFileDownloader downloader,
            Func<IMirrorSelector, ExportLifetimeContext<IMultiMirrorFileDownloader>> createMultiMirrorFileDownloader,
            Func<IReadOnlyCollection<Uri>, ExportLifetimeContext<IMirrorSelector>> createMirrorSelector,
            Func<int, IReadOnlyCollection<Uri>, ExportLifetimeContext<IMirrorSelector>> createMirrorSelectorWithLimit,
            Func<IMultiMirrorFileDownloader, ExportLifetimeContext<IFileQueueDownloader>> createQueueDownloader) {
            _downloader = downloader;
            _createMirrorSelector = createMirrorSelector;
            _createMirrorSelectorWithLimit = createMirrorSelectorWithLimit;
            _createMultiMirrorFileDownloader = createMultiMirrorFileDownloader;
            _createQueueDownloader = createQueueDownloader;
        }

        public Task DownloadFileAsync(Uri uri, IAbsoluteFilePath localFile) {
            localFile.MakeSureParentPathExists();
            return _downloader.DownloadAsync(uri, localFile);
        }

        public async Task DownloadFileAsync(string remoteFile, IAbsoluteDirectoryPath destinationPath,
            IReadOnlyCollection<Uri> remotes,
            CancellationToken token,
            int limit) {
            using (var scoreMirrorSelector = _createMirrorSelectorWithLimit(limit, remotes)) {
                await
                    DownloadFileAsync(remoteFile, destinationPath, scoreMirrorSelector.Value, token, x => true,
                        RepositoryRemote.CalculateHttpFallbackAfter(limit)).ConfigureAwait(false);
            }
        }

        public Task DownloadFileAsync(string remoteFile, IAbsoluteFilePath destinationPath,
                IMirrorSelector selector, ITransferStatus status, CancellationToken token)
            => DownloadFileAsync(remoteFile, destinationPath, selector, token, x => true, status);

        public ExportLifetimeContext<IMirrorSelector> StartMirrorSession(int limit, IReadOnlyCollection<Uri> remotes)
            => _createMirrorSelectorWithLimit(limit, remotes);

        public ExportLifetimeContext<IMirrorSelector> StartMirrorSession(IReadOnlyCollection<Uri> remotes)
            => _createMirrorSelector(remotes);

        public async Task DownloadFileAsync(string remoteFile, IAbsoluteDirectoryPath destinationPath,
            IReadOnlyCollection<Uri> remotes, CancellationToken token, int limit,
            Func<IAbsoluteFilePath, bool> confirmValidity) {
            using (var scoreMirrorSelector = _createMirrorSelectorWithLimit(limit, remotes)) {
                await
                    DownloadFileAsync(remoteFile, destinationPath, scoreMirrorSelector.Value, token, confirmValidity,
                            RepositoryRemote.CalculateHttpFallbackAfter(limit))
                        .ConfigureAwait(false);
            }
        }

        public async Task DownloadFilesAsync(IReadOnlyCollection<Uri> remotes, StatusRepo sr,
            IDictionary<FileFetchInfo, ITransferStatus> transferStatuses, IAbsoluteDirectoryPath destination) {
            sr.Action = RepoStatus.Downloading;
            using (var scoreMirrorSelector = _createMirrorSelector(remotes)) {
                await
                    DownloadFilesAsync(sr, transferStatuses, destination, scoreMirrorSelector.Value)
                        .ConfigureAwait(false);
            }
        }

        // NOTE: Must manually control AllowHttpZsyncFallbackAfter - generally based on the limit!
        public async Task DownloadFilesAsync(IReadOnlyCollection<Uri> remotes, StatusRepo sr,
            IDictionary<FileFetchInfo, ITransferStatus> transferStatuses,
            IAbsoluteDirectoryPath destination,
            int limit) {
            sr.Action = RepoStatus.Downloading;
            using (var scoreMirrorSelector = _createMirrorSelectorWithLimit(limit, remotes)) {
                await
                    DownloadFilesAsync(sr, transferStatuses, destination, scoreMirrorSelector.Value)
                        .ConfigureAwait(false);
            }
        }

        public Task DownloadFileAsync(string remoteFile, IAbsoluteDirectoryPath destinationPath,
                IMirrorSelector selector, int limit, CancellationToken token)
            => DownloadFileAsync(remoteFile, destinationPath, selector, token, x => true,
                RepositoryRemote.CalculateHttpFallbackAfter(limit));

        async Task DownloadFileAsync(string remoteFile, IAbsoluteDirectoryPath destinationPath,
            IMirrorSelector scoreMirrorSelector, CancellationToken token) {
            destinationPath.MakeSurePathExists();
            using (var dl = _createMultiMirrorFileDownloader(scoreMirrorSelector)) {
                await
                    dl.Value.DownloadAsync(new MultiMirrorFileDownloadSpec(remoteFile,
                            destinationPath.GetChildFileWithName(remoteFile)) {CancellationToken = token}, token)
                        .ConfigureAwait(false);
            }
        }

        Task DownloadFileAsync(string remoteFile, IAbsoluteDirectoryPath destinationPath,
                IMirrorSelector scoreMirrorSelector, CancellationToken token,
                Func<IAbsoluteFilePath, bool> confirmValidity, int zsyncHttpFallbackAfter)
            => DownloadFileAsync(remoteFile, destinationPath.GetChildFileWithName(remoteFile), scoreMirrorSelector,
                token, confirmValidity, zsyncHttpFallbackAfter);

        Task DownloadFileAsync(string remoteFile, IAbsoluteFilePath destinationPath,
                IMirrorSelector scoreMirrorSelector, CancellationToken token,
                Func<IAbsoluteFilePath, bool> confirmValidity, int zsyncHttpFallbackAfter)
            => DownloadFileAsync(remoteFile, destinationPath, scoreMirrorSelector, token, confirmValidity,
                new TransferStatus(remoteFile) {ZsyncHttpFallbackAfter = zsyncHttpFallbackAfter});

        async Task DownloadFileAsync(string remoteFile, IAbsoluteFilePath destinationPath,
            IMirrorSelector scoreMirrorSelector, CancellationToken token,
            Func<IAbsoluteFilePath, bool> confirmValidity, ITransferStatus status) {
            destinationPath.MakeSureParentPathExists();
            using (var dl = _createMultiMirrorFileDownloader(scoreMirrorSelector)) {
                await
                    dl.Value.DownloadAsync(new MultiMirrorFileDownloadSpec(remoteFile,
                            destinationPath, confirmValidity) {
                            CancellationToken = token,
                            Progress = status
                        },
                        token).ConfigureAwait(false);
            }
        }

        async Task DownloadFilesAsync(StatusRepo sr,
            IDictionary<FileFetchInfo, ITransferStatus> transferStatuses,
            IAbsoluteDirectoryPath destinationPath, IMirrorSelector scoreMirrorSelector) {
            destinationPath.MakeSurePathExists();
            sr.Total = transferStatuses.Count;
            using (var multiMirrorFileDownloader = _createMultiMirrorFileDownloader(scoreMirrorSelector))
            using (var multi = _createQueueDownloader(multiMirrorFileDownloader.Value)) {
                await multi.Value
                    .DownloadAsync(CreateFileQueueSpec(transferStatuses, destinationPath), sr.CancelToken)
                    .ConfigureAwait(false);
            }
        }

        static FileQueueSpec CreateFileQueueSpec(
            IDictionary<FileFetchInfo, ITransferStatus> transferStatuses,
            IAbsoluteDirectoryPath destination) => new FileQueueSpec(transferStatuses, destination);
    }
}