// <copyright company="SIX Networks GmbH" file="Synqer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services;
using SN.withSIX.Sync.Core.Legacy.Status;
using SN.withSIX.Sync.Core.Packages.Internals;
using SN.withSIX.Sync.Core.Repositories.Internals;
using SN.withSIX.Sync.Core.Transfer;
using SN.withSIX.Sync.Core.Transfer.MirrorSelectors;

namespace SN.withSIX.Sync.Core.Packages
{
    public interface ISynqer
    {
        Task DownloadPackages(Package[] p,
            IAbsoluteDirectoryPath downloadPath, Uri[] hosts, StatusRepo status, ProgressLeaf cleanup,
            ProgressContainer progress);

        Task DownloadPackages(Dictionary<IAbsoluteDirectoryPath, FileObjectMapping[]> dict,
            IAbsoluteDirectoryPath downloadPath, Uri[] hosts, StatusRepo status, ProgressLeaf cleanup,
            ProgressContainer progress);
    }

    public class Synqer : IDomainService, ISynqer
    {
        private static readonly string[] excludeFiles = {".synqinfo"};

        public Task DownloadPackages(Package[] p,
            IAbsoluteDirectoryPath downloadPath, Uri[] hosts, StatusRepo status, ProgressLeaf cleanup,
            ProgressContainer progress)
            => DownloadPackages(TransformPackages(p), downloadPath, hosts, status, cleanup, progress);

        public async Task DownloadPackages(Dictionary<IAbsoluteDirectoryPath, FileObjectMapping[]> dict,
            IAbsoluteDirectoryPath downloadPath, Uri[] hosts, StatusRepo status, ProgressLeaf cleanup,
            ProgressContainer progress) {
            await ProcessChanges(dict, downloadPath, hosts, status, progress).ConfigureAwait(false);
            // find all files that are too many, and delete them
            if (cleanup != null)
                PerformCleanup(dict, cleanup);
        }

        private Task ProcessChanges(Dictionary<IAbsoluteDirectoryPath, FileObjectMapping[]> dict,
            IAbsoluteDirectoryPath downloadPath, Uri[] hosts, StatusRepo status,
            ProgressContainer progress) {
            var files = PrepareFiles(dict, downloadPath, hosts, status, progress);
            return Process(files, downloadPath, hosts, status.CancelToken, SyncEvilGlobal.Limiter());
        }

        private static FileInfoWithData<State>[] PrepareFiles(
            Dictionary<IAbsoluteDirectoryPath, FileObjectMapping[]> dict, IAbsoluteDirectoryPath downloadPath,
            Uri[] hosts,
            StatusRepo status, ProgressContainer component) {
            var map = new Dictionary<string, List<IAbsoluteFilePath>>();

            foreach (var a in dict) {
                foreach (var v in a.Value) {
                    var fullPath = a.Key.GetChildFileWithName(v.FilePath);
                    if (map.ContainsKey(v.Checksum))
                        map[v.Checksum].Add(fullPath);
                    else
                        map[v.Checksum] = new List<IAbsoluteFilePath> {fullPath};
                }
            }

            return map.Select(x => {
                var progressComponent = new ProgressComponent(x.Key);
                component.AddComponents(progressComponent);
                return new FileInfoWithData<State>(
                    new DownloadInfo(GetRemotePath(x.Key),
                        downloadPath.GetChildFileWithName(SplitObjectName(x.Key)), hosts),
                    x.Value.ToArray(),
                    new State {
                        Checksum = x.Key,
                        Progress = new Progress(progressComponent)
                    }) {
                        CancelToken = status.CancelToken
                    };
            }).OrderByDescending(x => Tools.FileUtil.SizePrediction(x.Destinations.First().FileName)).ToArray();
        }

        Dictionary<IAbsoluteDirectoryPath, FileObjectMapping[]> TransformPackages(IEnumerable<Package> p)
            => p.ToDictionary(x => x.WorkingPath,
                x => x.MetaData.Files.Select(f => new FileObjectMapping(f.Key, f.Value)).ToArray());

        private static string GetRemotePath(string objectName) => "objects/" + SplitObjectName(objectName);

        private static string SplitObjectName(string objectName)
            => objectName.Substring(0, 2) + "/" + objectName.Substring(2);

        private async Task Process(IReadOnlyCollection<FileInfoWithData<State>> files,
            IAbsoluteDirectoryPath downloadPath,
            IReadOnlyCollection<Uri> mirrors, CancellationToken cancellationToken, int concurrentDownloads) {
            using (var processor = new SynqProcessor(concurrentDownloads, cancellationToken))
            using (var selector = SyncEvilGlobal.DownloadHelper.StartMirrorSession(mirrors))
                await new Handler(processor, selector.Value).DoIt(files).ConfigureAwait(false);
            if (downloadPath.Exists)
                downloadPath.DirectoryInfo.Delete(true);
        }

        private static void PerformCleanup(Dictionary<IAbsoluteDirectoryPath, FileObjectMapping[]> dict,
            ProgressLeaf status) => status.Do(() => PerformCleanupInternal(dict, status));

        private static void PerformCleanupInternal(Dictionary<IAbsoluteDirectoryPath, FileObjectMapping[]> dict,
            IUpdateSpeedAndProgress status) {
            var i = 0;
            foreach (var folder in dict) {
                CleanupPackage(folder.Key, folder.Value.Select(x => x.FilePath));
                status.Update(null, i++.ToProgress(dict.Count));
            }
        }

        private static void CleanupPackage(IAbsoluteDirectoryPath packageDirectory, IEnumerable<string> packageFiles) {
            // Find all unneeded files and delete them
            var neededFiles =
                packageFiles
                    .Concat(excludeFiles)
                    .Select(packageDirectory.GetChildFileWithName);
            foreach (
                var f in
                    packageDirectory.DirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                        .Select(x => x.ToAbsoluteFilePath())
                        .Except(neededFiles)
                        .Where(x => {
                            var relative = x.GetRelativePathFrom(packageDirectory);
                            return !relative.SubStartsWith(".");
                        })) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.Info($"Deleting {f}");
                f.Delete();
            }

            // Find all empty directories and delete them
            foreach (
                var d in
                    packageDirectory.DirectoryInfo.EnumerateDirectories("*", SearchOption.AllDirectories).Reverse()
                        .Select(x => x.ToAbsoluteDirectoryPath())
                        .Where(x => {
                            var relative = x.GetRelativePathFrom(packageDirectory);
                            return !relative.SubStartsWith(".");
                        })
                        .Where(x => !x.DirectoryInfo.EnumerateFileSystemInfos().Any())
                ) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.Info($"Deleting directory {d}");
                d.Delete();
            }
        }

        class Progress
        {
            private readonly ProgressComponent _component;
            private IProgressComponent[] _components;

            public Progress(ProgressComponent component) {
                _component = component;
                _components = new IProgressComponent[] {
                    Checking = new ProgressLeaf("Checking"),
                    Compressing = new ProgressLeaf("Compressing", 3),
                    Copying = new ProgressContainer("Copying", 2),
                    Downloading = new ProgressLeaf("Downloading", 9),
                    Extracting = new ProgressContainer("Extracting", 3)
                };
                _component.AddComponents(_components);
            }

            public ProgressLeaf Checking { get; }

            public ProgressContainer Copying { get; }

            public ProgressContainer Extracting { get; }

            public ProgressLeaf Downloading { get; }

            public ProgressLeaf Compressing { get; }

            public void RemoveComponentsExcept(params IProgressComponent[] leafs)
                => _component.RemoveComponents(_components = _components.Except(leafs).ToArray());
        }

        class SynqProcessor : IDisposable
        {
            private readonly QueueInfoWithExceptions _compressor;

            private readonly QueueInfoWithExceptions _decompressor;

            private readonly QueueInfoWithExceptions _downloader;

            private readonly QueueInfoWithExceptions _preparer;
            private readonly List<QueueInfoWithExceptions> _queues = new List<QueueInfoWithExceptions>();
            private Task _run;

            public SynqProcessor(int concurrentDownloads, CancellationToken cancellationToken) {
                _queues.Add(_preparer = new QueueInfoWithExceptions());
                _queues.Add(_compressor = new QueueInfoWithExceptions());
                _queues.Add(_downloader = new QueueInfoWithExceptions(concurrentDownloads));
                _queues.Add(_decompressor = new QueueInfoWithExceptions());
                var processor = new Processor();
                _run = processor.Run(cancellationToken, _queues.ToArray());
            }

            public void Dispose() {
                foreach (var q in _queues)
                    q.Dispose();
            }

            public void AddToDecompressor(Action t) => _decompressor.AddWithExceptionHandling(async () => t());
            public void AddToCompressor(Action t) => _compressor.AddWithExceptionHandling(async () => t());
            public void AddToDownloader(Action t) => _downloader.AddWithExceptionHandling(async () => t());
            public void AddToPreparer(Action t) => _preparer.AddWithExceptionHandling(async () => t());

            public void AddToDecompressor(Func<Task> t) => _decompressor.AddWithExceptionHandling(t);
            public void AddToCompressor(Func<Task> t) => _compressor.AddWithExceptionHandling(t);
            public void AddToDownloader(Func<Task> t) => _downloader.AddWithExceptionHandling(t);
            public void AddToPreparer(Func<Task> t) => _preparer.AddWithExceptionHandling(t);

            public async Task MakeSession(Action act) {
                act();
                _preparer.CompleteAdding();
                // if we don't call this, then it would run indefinitely waiting for more objects
                await Commit().ConfigureAwait(false);
            }

            private async Task Commit() {
                var run = _run;
                _run = null;
                await run;
                QueueInfoWithExceptions.ConfirmSuccess(_queues.ToArray());
            }
        }

        class Handler
        {
            private readonly SynqProcessor _processor;
            private readonly IMirrorSelector _selector;

            public Handler(SynqProcessor processor, IMirrorSelector selector) {
                _processor = processor;
                _selector = selector;
            }

            public Task DoIt(IEnumerable<FileInfoWithData<State>> files) => _processor.MakeSession(() => {
                //PreCreatePaths(files);
                foreach (var f in files) {
                    if (f.Destinations.Any(x => x.Exists))
                        _processor.AddToPreparer(() => f.Data.Progress.Checking.Do(() => HandleCheckingExisting(f)));
                    else {
                        f.Data.Progress.RemoveComponentsExcept(f.Data.Progress.Downloading, f.Data.Progress.Extracting);
                        _processor.AddToDownloader(() => {
                            f.Data.UpdateExistingObject = f.DownloadInfo.LocalFilePath.Exists;
                            return HandleDownload(f);
                        });
                    }
                }
            });

            private static void PreCreatePaths(IReadOnlyCollection<FileInfoWithData<State>> files) {
                var created = new HashSet<IAbsoluteDirectoryPath>();
                foreach (
                    var parent in
                        files.SelectMany(
                            f => f.Destinations.Select(fullPath => fullPath.ParentDirectoryPath).Distinct())
                            .Distinct()
                            .Where(parent => !created.Contains(parent))) {
                    parent.MakeSurePathExists();
                    created.Add(parent);
                }
            }

            private void HandleCheckingExisting(FileInfoWithData<State> f) {
                var existing =
                    f.Destinations.Where(x => x.Exists).OrderByDescending(x => x.FileInfo.CreationTimeUtc).ToArray();
                var existingCorrect =
                    existing.Where(x => Tools.HashEncryption.SHA1FileHash(x) == f.Data.Checksum).ToArray();
                if (existingCorrect.Any()) {
                    HandleCorrectExistingFiles(f, existingCorrect);
                    return;
                }

                // TODO: Only when remotes support resume (zsync/rsync)
                if (f.DownloadInfo.LocalFilePath.Exists) {
                    f.Data.Progress.RemoveComponentsExcept(f.Data.Progress.Checking, f.Data.Progress.Downloading,
                        f.Data.Progress.Extracting);
                    f.Data.UpdateExistingObject = true;
                    _processor.AddToDownloader(() => HandleDownload(f));
                } else {
                    var firstExistingFile = existing.First();
                    f.Data.Progress.RemoveComponentsExcept(f.Data.Progress.Checking, f.Data.Progress.Compressing,
                        f.Data.Progress.Downloading,
                        f.Data.Progress.Extracting);
                    _processor.AddToCompressor(
                        () => f.Data.Progress.Compressing.Do(() => HandleCompressionInternal(f, firstExistingFile)));
                }
            }

            private void HandleCorrectExistingFiles(FileInfoWithData<State> f,
                IAbsoluteFilePath[] existingCorrect) {
                var todo = f.Destinations.Except(existingCorrect).ToArray();
                if (!todo.Any()) {
                    f.Data.Progress.RemoveComponentsExcept(f.Data.Progress.Checking);
                    // todo we could add checking state before hand, but then we should reverse the process of addcomponents:
                    // remove the additional stages when they are not needed..
                    return;
                }
                var file = existingCorrect.First();
                f.Data.Progress.RemoveComponentsExcept(f.Data.Progress.Checking, f.Data.Progress.Copying);
                _processor.AddToDecompressor(() => HandleCopying(file, todo, f));
            }

            private async Task HandleCopying(IAbsoluteFilePath file, IAbsoluteFilePath[] todo, FileInfoWithData<State> f) {
                var c = todo.Select(x => new ProgressLeaf(x.FileName)).ToArray();
                f.Data.Progress.Copying.AddComponents(c);
                var i = 0;
                foreach (var dst in todo) {
                    var s = c[i++];
                    await s.Do(() => HandleCopyFileInternal(file, dst, s)).ConfigureAwait(false);
                }
            }

            private static Task HandleCopyFileInternal(IAbsoluteFilePath file, IAbsoluteFilePath dst, ITProgress status) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.Info($"Copying {file} to {dst}");
                dst.MakeSureParentPathExists();
                return file.CopyAsync(dst, status: status);
            }

            private void HandleCompressionInternal(FileInfoWithData<State> f, IAbsoluteFilePath src) {
                f.DownloadInfo.LocalFilePath.MakeSureParentPathExists();
                if (Common.Flags.Verbose)
                    MainLog.Logger.Info($"Compressing {src} to {f.DownloadInfo.LocalFilePath}");
                Tools.Compression.Gzip.GzipAuto(src, f.DownloadInfo.LocalFilePath, true, f.Data.Progress.Compressing);
                f.Data.UpdateExistingObject = true;
                _processor.AddToDownloader(() => HandleDownload(f));
            }

            private async Task HandleDownload(FileInfoWithData<State> f) {
                // TODO: Monitor the download progress, and increase the progress accordingly (80% dl, 20% extract) :S
                // then turn off the progress monitoring of StatusRepo again
                await f.Data.Progress.Downloading.Do(() => HandleDownloadInternal(f)).ConfigureAwait(false);
                _processor.AddToDecompressor(() => HandleExtraction(f));
            }

            private async Task HandleDownloadInternal(FileInfoWithData<State> f) {
                if (Common.Flags.Verbose) {
                    MainLog.Logger.Info(
                        $"Downloading {f.DownloadInfo.RemoteFilePath} to {f.DownloadInfo.LocalFilePath} (for {string.Join(", ", f.Destinations)}). ExistingObject? {f.Data.UpdateExistingObject}");
                }
                var status = f.Data.Progress.Downloading;
                await
                    SyncEvilGlobal.DownloadHelper.DownloadFileAsync(f.DownloadInfo.RemoteFilePath,
                        f.DownloadInfo.LocalFilePath, _selector,
                        new StatusWrapper2(new TransferStatus(f.DownloadInfo.RemoteFilePath),
                            (x, y) => status.Update(y, x)), f.CancelToken).ConfigureAwait(false);
            }

            private void HandleExtraction(FileInfoWithData<State> f) {
                var status = f.Data.Progress.Extracting;
                var i = 0;
                var co = f.Destinations.Select(x => new ProgressLeaf(x.FileName)).ToArray();
                status.AddComponents(co);
                foreach (var c in f.Destinations) {
                    var s = co[i++];
                    s.Do(() => HandleExtractionDestination(f, c, s));
                }
                if (Common.Flags.Verbose)
                    MainLog.Logger.Info($"Deleting temporary download file {f.DownloadInfo.LocalFilePath}");
                f.DownloadInfo.LocalFilePath.FileInfo.Delete();
            }

            private static void HandleExtractionDestination(FileInfoWithData<State> f, IAbsoluteFilePath c,
                ITProgress s) {
                var tmpFile = (c + ".six-tmp").ToAbsoluteFilePath();
                if (Common.Flags.Verbose) {
                    MainLog.Logger.Info(
                        $"Decompressing {f.DownloadInfo.LocalFilePath} to {c} (for {string.Join(", ", f.Destinations)})");
                }
                tmpFile.MakeSureParentPathExists();
                Tools.Compression.Gzip.UnpackSingleGzip(f.DownloadInfo.LocalFilePath, tmpFile, s);
                if (Tools.HashEncryption.SHA1FileHash(tmpFile) != f.Data.Checksum)
                    throw new ChecksumException("The checksum does not match");
                tmpFile.Move(c);
            }
        }

        public class FileInfo
        {
            public FileInfo(DownloadInfo downloadInfo, IReadOnlyCollection<IAbsoluteFilePath> destinations) {
                DownloadInfo = downloadInfo;
                Destinations = destinations;
            }

            public DownloadInfo DownloadInfo { get; }
            public IReadOnlyCollection<IAbsoluteFilePath> Destinations { get; }
        }

        public class FileInfoWithData<T> : FileInfo
        {
            public FileInfoWithData(DownloadInfo downloadInfo, IReadOnlyCollection<IAbsoluteFilePath> destinations,
                T data)
                : base(downloadInfo, destinations) {
                Data = data;
            }

            public T Data { get; }
            public CancellationToken CancelToken { get; set; }
        }

        public class DownloadInfo
        {
            public DownloadInfo(string remoteFilePath, IAbsoluteFilePath localFilePath, Uri[] hosts) {
                RemoteFilePath = remoteFilePath;
                LocalFilePath = localFilePath;
                Hosts = hosts;
            }

            public string RemoteFilePath { get; }
            public IAbsoluteFilePath LocalFilePath { get; }
            public Uri[] Hosts { get; }
        }

        public class QueueInfo<T> : BlockingCollection<T>
        {
            public QueueInfo(int maxThreads = 1) {
                MaxThreads = maxThreads;
            }

            public int MaxThreads { get; }
        }

        public class QueueInfoWithExceptions : QueueInfo<Func<Task>>
        {
            public QueueInfoWithExceptions(int maxThreads = 1) : base(maxThreads) {}

            private ConcurrentBag<Exception> Exceptions { get; } = new ConcurrentBag<Exception>();

            public bool HasExceptions() => Exceptions.Any();

            public AggregateException GetExceptions() => HasExceptions() ? new AggregateException(Exceptions) : null;

            public static AggregateException MergeExceptions(params QueueInfoWithExceptions[] queues) {
                var ex = queues.SelectMany(x => x.Exceptions).ToArray();
                return ex.Any()
                    ? new QueueProcessingError("One or more errors occurred while trying to process content", ex)
                    : null;
            }

            public static void ConfirmSuccess(params QueueInfoWithExceptions[] queues) {
                var ex = MergeExceptions(queues);
                if (ex != null)
                    throw ex;
            }

            public void ConfirmSuccess() {
                if (HasExceptions())
                    throw GetExceptions();
            }

            public void AddWithExceptionHandling(Func<Task> task) {
                Add(async () => {
                    try {
                        await task();
                    } catch (Exception ex) {
                        Exceptions.Add(ex);
                    }
                });
            }
        }

        public class Processor
        {
            private int _i;
            private QueueInfo<Func<Task>>[] _queues;

            public Task Run(CancellationToken cancelToken, params QueueInfo<Func<Task>>[] queues)
                => Run(queues, cancelToken);

            public Task Run(QueueInfo<Func<Task>>[] queues, CancellationToken cancelToken) {
                _queues = queues;
                return Task.WhenAll(_queues.Select(x => ProcessQueue(x, cancelToken, x.MaxThreads)));
            }

            private async Task ProcessQueue(BlockingCollection<Func<Task>> b, CancellationToken cancelToken,
                int maxThreads = 1) {
                var id = _i++;
                try {
                    await b.RunningQueue(maxThreads, cancelToken);
                } finally {
                    var nextQ = _queues.Length > id + 1 ? _queues[id + 1] : null;
                    nextQ?.CompleteAdding();
                }
            }
        }

        class State
        {
            public string Checksum { get; set; }
            public bool Success { get; set; }
            public Progress Progress { get; set; }
            public bool UpdateExistingObject { get; set; }
        }
    }

    public class QueueProcessingError : AggregateException
    {
        public QueueProcessingError(string message, Exception[] exceptions) : base(message, exceptions) {}
    }
}