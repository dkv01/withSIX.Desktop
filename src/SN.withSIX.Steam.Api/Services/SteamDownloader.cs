// <copyright company="SIX Networks GmbH" file="SteamDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services;
using SN.withSIX.Steam.Api.Helpers;
using Steamworks;

namespace SN.withSIX.Steam.Api.Services
{
    public class SteamDownloader : ISteamDownloader, IDomainService
    {
        private readonly ISteamApi _api;

        public SteamDownloader(ISteamApi api) {
            _api = api;
        }

        public Task Download(PublishedFile pf, Action<long?, double> progressAction = null,
            CancellationToken cancelToken = default(CancellationToken)) {
            if (progressAction != null)
                HandleProgress(progressAction);
            return
                PerformDownload(pf, progressAction != null ? HandleProgress(progressAction) : null, cancelToken);
        }

        private static Action<DownloadInfo> HandleProgress(Action<long?, double> progressAction) {
            var processor = new Progress();
            Action<DownloadInfo> pCb = x => {
                if ((x.Total > 0) && (x.Downloaded == x.Total))
                    progressAction.Invoke(null, 100);
                else {
                    long? speed;
                    double progress;
                    processor.Process(x.Downloaded, x.Total, out speed, out progress);
                    progressAction.Invoke(speed, progress);
                }
            };
            return pCb;
        }

        private async Task PerformDownload(PublishedFile pf, Action<DownloadInfo> pCb, CancellationToken cancelToken) {
            var readySignal = CreateReadySignal(pf, cancelToken);
            // in case we get a result before we would be waiting for it..
            var readySignalTask = readySignal.ToTask(cancelToken);
            DownloadAndConfirm(pf);
            using (pCb == null ? null : ProcessDownloadInfo(pf, pCb, readySignal))
                await readySignalTask.ConfigureAwait(false);
        }

        // Generally we get the InstalledItem event first, and very shortly after we get the DownloadItemResult
        private IObservable<Unit> CreateReadySignal(PublishedFile pf, CancellationToken cancelToken)
            => CreateDownloadItemResultCompletionSource(pf, cancelToken)
                .Merge(CreateInstalledFileCompletionSource(pf, cancelToken), _api.Scheduler)
                //.Merge(CreateProgressCompletionSource(pf), _api.Scheduler)
                .Merge(CreateTimeoutSource(pf), _api.Scheduler)
                .Take(1);

        private IObservable<Unit> CreateDownloadItemResultCompletionSource(PublishedFile pf,
                CancellationToken cancelToken)
            => ObserveDownloadItemResultForApp(pf, cancelToken)
                .Do(x => MainLog.Logger.Info($"Received DownloadItemResult event for {pf}"))
                .Do(x => _api.ConfirmResult(x.m_eResult))
                .Void();

        private IObservable<Unit> CreateInstalledFileCompletionSource(PublishedFile pf, CancellationToken cancelToken)
            => ObserveInstalledFileForApp(pf, cancelToken)
                .Do(x => MainLog.Logger.Info($"Received InstalledFile event for {pf}"))
                .Void();

        private IObservable<Unit> CreateTimeoutSource(PublishedFile pf)
            => ObserveDownloadInfo(pf.Pid)
                .Throttle(TimeSpan.FromMinutes(2))
                .Do(x => MainLog.Logger.Info($"Reached timeout for {pf}"))
                .Do(x => {
                    throw new TimeoutException(
                        "Did not receive download progress info from Steam for over 60 seconds");
                })
                .Void();

        private IDisposable ProcessDownloadInfo(PublishedFile pf, Action<DownloadInfo> pCb,
            IObservable<Unit> readySignal) => ProcessDownloadInfo(pf.Pid, pCb, readySignal);

        private IDisposable ProcessDownloadInfo(PublishedFileId_t pid, Action<DownloadInfo> pCb,
            IObservable<Unit> readySignal) => ObserveDownloadInfo(pid)
            .TakeUntil(readySignal) // So that the completion makes us emit a last 100% progress item :)
            .Subscribe(pCb, () => pCb(new DownloadInfo(1, 1)));

        // however since we probably already unsubscribe before completion it doesn't really help :)

        private IObservable<DownloadItemResult_t> ObserveDownloadItemResultForApp(PublishedFile pf,
                CancellationToken cancelToken) =>
            ObserveDownloadItemResultForApp(pf.Aid, pf.Pid, cancelToken);

        private IObservable<DownloadItemResult_t> ObserveDownloadItemResultForApp(AppId_t aid, PublishedFileId_t pid,
                CancellationToken cancelToken)
            => ObserveDownloadItemResultForGame(aid, cancelToken)
                .Where(x => x.m_nPublishedFileId == pid);

        private IObservable<DownloadInfo> ObserveDownloadInfo(PublishedFileId_t pid)
            => Observable.Interval(TimeSpan.FromMilliseconds(500), _api.Scheduler)
                .Select(_ => GetDownloadInfo(pid))
                .DistinctUntilChanged();

        private static DownloadInfo GetDownloadInfo(PublishedFileId_t pid) {
            try {
                ulong downloaded;
                ulong total;
                if (!SteamUGC.GetItemDownloadInfo(pid, out downloaded, out total))
                    throw new InvalidOperationException("Cannot retrieve download info for " + pid);
                return new DownloadInfo(downloaded, total);
            } catch (Exception) {
                throw;
            } catch {
                throw new InvalidOperationException("Received a native exception");
            }
        }

        private static void DownloadAndConfirm(PublishedFile pf) {
            if (!SteamUGC.DownloadItem(pf.Pid, true))
                throw new InvalidOperationException("Failed to initiate download");
        }

        private IObservable<ItemInstalled_t> ObserveInstalledFileForApp(PublishedFile pf, CancellationToken cancelToken)
            => ObserveInstalledFileForApp(pf.Aid, pf.Pid, cancelToken);

        private IObservable<ItemInstalled_t> ObserveInstalledFileForApp(AppId_t aid, PublishedFileId_t pid,
                CancellationToken cancelToken)
            => ObserveInstalledForGame(aid, cancelToken).Where(x => x.m_nPublishedFileId == pid);

        private IObservable<ItemInstalled_t> ObserveInstalledForGame(AppId_t aid, CancellationToken cancelToken)
            => _api.CreateObservableFromCallback<ItemInstalled_t>(cancelToken).Where(x => x.m_unAppID == aid);

        private IObservable<DownloadItemResult_t> ObserveDownloadItemResultForGame(AppId_t aid,
                CancellationToken cancelToken)
            => _api.CreateObservableFromCallback<DownloadItemResult_t>(cancelToken)
                .Where(x => x.m_unAppID == aid);

        class Progress
        {
            ulong _lastBytes;
            DateTime _lastTime = Tools.Generic.GetCurrentUtcDateTime;

            public void Process(ulong bytes, ulong total, out long? speed, out double progress) {
                var now = Tools.Generic.GetCurrentUtcDateTime;
                speed = null;
                progress = 0;
                if ((total == 0) || (bytes == 0)) {
                    _lastTime = now;
                    return;
                }
                var timeSpan = now - _lastTime;
                var bytesChange = bytes - _lastBytes;

                if (timeSpan.TotalMilliseconds > 0)
                    speed = (long) (bytesChange/(timeSpan.TotalMilliseconds/1000.0));
                _lastBytes = bytes;
                progress = bytes.ToProgress(total);
                _lastTime = now;
            }
        }
    }

    public interface ISteamDownloader
    {
        Task Download(PublishedFile pf, Action<long?, double> progressAction = null,
            CancellationToken cancelToken = default(CancellationToken));
    }

    public struct DownloadInfo : IEquatable<DownloadInfo>
    {
        public ulong Downloaded { get; }
        public ulong Total { get; }

        public DownloadInfo(ulong downloaded, ulong total) {
            Downloaded = downloaded;
            Total = total;
        }

        public override int GetHashCode() => Downloaded.GetHashCode() ^ Total.GetHashCode();

        public bool Equals(DownloadInfo other) => other.GetHashCode() == GetHashCode();
    }
}