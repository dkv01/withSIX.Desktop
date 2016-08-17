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
using SN.withSIX.Core.Services;
using Steamworks;

namespace SN.withSIX.Steam.Api
{
    public interface ISteamDownloader
    {
        Task Download(uint appId, ulong publishedFileId, Action<long?, double> progressAction = null,
            CancellationToken cancelToken = default(CancellationToken), bool force = false);
    }

    public class SteamDownloader : ISteamDownloader, IDomainService
    {
        public async Task Download(uint appId, ulong publishedFileId, Action<long?, double> progressAction = null,
            CancellationToken cancelToken = default(CancellationToken), bool force = false) {
            var pid = new PublishedFileId_t(publishedFileId);
            var state = (EItemState) SteamUGC.GetItemState(pid);
            if (!force && !state.RequiresDownloading()) {
                progressAction?.Invoke(null, 100);
                return;
            }

            await HandleSubscription(force, state, pid).ConfigureAwait(false);

            if (progressAction != null)
                HandleProgress(progressAction);

            await
                PerformDownload(appId, pid, progressAction != null ? HandleProgress(progressAction) : null, cancelToken)
                    .ConfigureAwait(false);
        }

        private static async Task HandleSubscription(bool force, EItemState state, PublishedFileId_t pid) {
            var isSubscribed = state.HasFlag(EItemState.k_EItemStateSubscribed);
            if (force && isSubscribed) {
                await UnsubscribeAndConfirm(pid).ConfigureAwait(false);
                isSubscribed = false;
            }
            if (!isSubscribed)
                await SubscribeAndConfirm(pid).ConfigureAwait(false);
        }

        private static Action<DownloadInfo> HandleProgress(Action<long?, double> progressAction) {
            var processor = new Progress();
            Action<DownloadInfo> pCb = x => {
                if (x.Total > 0 && x.Downloaded == x.Total)
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

        private async Task PerformDownload(uint appId, PublishedFileId_t pid, Action<DownloadInfo> pCb,
            CancellationToken cancelToken) {
            var aid = new AppId_t(appId);

            // TODO: Timeout based on receiving Download progress...
            var readySignal = ObserveDownloadItemResultForApp(aid, pid, cancelToken)
                .Select(x => { ConfirmResult(x.m_eResult); return Unit.Default; })
                .Merge(ObserveInstalledFileForApp(aid, pid, cancelToken).Select(x => Unit.Default))
                .Take(1);

            // in case we get a result before we would be waiting for it..
            var readySignalTask = readySignal.ToTask(cancelToken);
            DownloadAndConfirm(pid);
            using (pCb == null ? null : ProcessDownloadInfo(pid, pCb, readySignal))
                await readySignalTask.ConfigureAwait(false);
        }

        private static IDisposable ProcessDownloadInfo(PublishedFileId_t pid, Action<DownloadInfo> pCb,
            IObservable<Unit> obs2) => ObserveDownloadInfo(pid)
                .TakeUntil(obs2)
                .Subscribe(pCb, () => pCb(new DownloadInfo(ulong.MaxValue, ulong.MaxValue)));

        private IObservable<DownloadItemResult_t> ObserveDownloadItemResultForApp(AppId_t aid, PublishedFileId_t pid,
            CancellationToken cancelToken)
            => ObserveDownloadItemResultForGame(aid, cancelToken)
                .Where(x => x.m_nPublishedFileId == pid);

        private static IObservable<DownloadInfo> ObserveDownloadInfo(PublishedFileId_t pid)
            => Observable.Interval(TimeSpan.FromMilliseconds(500), SteamHelper.Scheduler)
                .Select(_ => GetPublishedFileState(pid))
                .DistinctUntilChanged();

        private static DownloadInfo GetPublishedFileState(PublishedFileId_t pid) {
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

        private static void DownloadAndConfirm(PublishedFileId_t pid) {
            if (!SteamUGC.DownloadItem(pid, true))
                throw new InvalidOperationException("Failed to initiate download");
        }

        private static async Task SubscribeAndConfirm(PublishedFileId_t pid) {
            var r = await SubscribeToContent(pid);
            ConfirmResult(r.m_eResult);
        }

        private static async Task UnsubscribeAndConfirm(PublishedFileId_t pid) {
            var r = await UnsubscribeFromContent(pid);
            ConfirmResult(r.m_eResult);
        }

        private static void ConfirmResult(EResult mEResult) {
            if (mEResult != EResult.k_EResultOK)
                throw new InvalidOperationException("Failed with result " + mEResult);
        }

        private IObservable<ItemInstalled_t> ObserveInstalledFileForApp(AppId_t aid, PublishedFileId_t pid,
            CancellationToken cancelToken)
            => ObserveInstalledForGame(aid, cancelToken).Where(x => x.m_nPublishedFileId == pid);

        private IObservable<ItemInstalled_t> ObserveInstalledForGame(AppId_t aid, CancellationToken cancelToken)
            => SteamHelper.CreateObservableFromCallback<ItemInstalled_t>(cancelToken).Where(x => x.m_unAppID == aid);

        private IObservable<DownloadItemResult_t> ObserveDownloadItemResultForGame(AppId_t aid,
            CancellationToken cancelToken)
            =>
                SteamHelper.CreateObservableFromCallback<DownloadItemResult_t>(cancelToken)
                    .Where(x => x.m_unAppID == aid);

        private static IObservable<RemoteStorageSubscribePublishedFileResult_t> SubscribeToContent(PublishedFileId_t pid)
            =>
                SteamUGC.SubscribeItem(pid)
                    .CreateObservableFromCallresults<RemoteStorageSubscribePublishedFileResult_t>()
                    .Take(1);

        private static IObservable<RemoteStorageUnsubscribePublishedFileResult_t> UnsubscribeFromContent(
            PublishedFileId_t pid)
            =>
                SteamUGC.UnsubscribeItem(pid)
                    .CreateObservableFromCallresults<RemoteStorageUnsubscribePublishedFileResult_t>()
                    .Take(1);

        class Progress
        {
            ulong _lastBytes;
            DateTime _lastTime = Tools.Generic.GetCurrentUtcDateTime;

            public void Process(ulong bytes, ulong total, out long? speed, out double progress) {
                var now = Tools.Generic.GetCurrentUtcDateTime;
                speed = null;
                progress = 0;
                if (total == 0 || bytes == 0) {
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

    public static class Extensions
    {
        public static bool RequiresDownloading(this EItemState state)
            => !state.HasFlag(EItemState.k_EItemStateInstalled) || state.HasFlag(EItemState.k_EItemStateNeedsUpdate);
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