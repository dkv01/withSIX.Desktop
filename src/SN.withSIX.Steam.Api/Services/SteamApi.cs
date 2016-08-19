// <copyright company="SIX Networks GmbH" file="SteamApi.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using SN.withSIX.Core.Services;
using SN.withSIX.Steam.Api.Helpers;
using Steamworks;

namespace SN.withSIX.Steam.Api.Services
{
    public class SteamApi : ISteamApi, IDomainService
    {
        private readonly ISteamSessionLocator _sessionLocator;

        public SteamApi(ISteamSessionLocator sessionLocator) {
            _sessionLocator = sessionLocator;
        }

        public async Task SubscribeAndConfirm(PublishedFileId_t pid) {
            var r = await SubscribeToContent(pid);
            ConfirmResult(r.m_eResult);
        }

        public async Task UnsubscribeAndConfirm(PublishedFileId_t pid) {
            var r = await UnsubscribeFromContent(pid);
            ConfirmResult(r.m_eResult);
        }

        public void ConfirmResult(EResult mEResult) {
            if (mEResult != EResult.k_EResultOK)
                throw new FailedOperationException("Failed with result " + mEResult, mEResult);
        }

        public IScheduler Scheduler => _sessionLocator.Session.Scheduler;

        public ItemInstallInfo GetItemInstallInfo(PublishedFileId_t pid) {
            ulong sizeOnDisk;
            string folder;
            uint folderSize = 0; // ?
            uint timestamp;
            return SteamUGC.GetItemInstallInfo(pid, out sizeOnDisk, out folder, folderSize, out timestamp)
                ? new ItemInstallInfo(folder, sizeOnDisk, folderSize, timestamp)
                : null;
            //throw new InvalidOperationException("Item is not actually considered installed?");
        }

        public IObservable<T> CreateObservableFromCallback<T>(CancellationToken cancelToken)
            => Observable.Create<T>(observer => {
                var callback = Callback<T>.Create(observer.OnNext);
                var r = cancelToken.Register(observer.HandleCanceled);
                return () => {
                    callback.Unregister();
                    r.Dispose();
                };
            }).ObserveOn(_sessionLocator.Session.Scheduler);

        private IObservable<RemoteStorageSubscribePublishedFileResult_t> SubscribeToContent(PublishedFileId_t pid)
            =>
                SteamUGC.SubscribeItem(pid)
                    .CreateObservableFromCallresults<RemoteStorageSubscribePublishedFileResult_t>(
                        _sessionLocator.Session)
                    .Take(1);

        private IObservable<RemoteStorageUnsubscribePublishedFileResult_t> UnsubscribeFromContent(
            PublishedFileId_t pid)
            =>
                SteamUGC.UnsubscribeItem(pid)
                    .CreateObservableFromCallresults<RemoteStorageUnsubscribePublishedFileResult_t>(
                        _sessionLocator.Session)
                    .Take(1);

        public IObservable<T> CreateObservableFromCallback<T>()
            => Observable.Create<T>(observer => {
                var callback = Callback<T>.Create(observer.OnNext);
                return callback.Unregister;
            }).ObserveOn(_sessionLocator.Session.Scheduler);
    }

    public class ItemInstallInfo
    {
        public ItemInstallInfo(string location, ulong sizeOnDisk, uint folderSize, uint timestamp) {
            Location = location;
            SizeOnDisk = sizeOnDisk;
            FolderSize = folderSize;
            Timestamp = timestamp;
        }

        public string Location { get; }
        public ulong SizeOnDisk { get; }
        public uint FolderSize { get; }
        public uint Timestamp { get; }
    }

    class FailedOperationException : InvalidOperationException
    {
        public FailedOperationException(string message, EResult result) : base(message) {
            Result = result;
        }

        public EResult Result { get; }
    }

    public interface ISteamApi
    {
        IScheduler Scheduler { get; }
        Task SubscribeAndConfirm(PublishedFileId_t pid);
        Task UnsubscribeAndConfirm(PublishedFileId_t pid);
        void ConfirmResult(EResult mEResult);
        IObservable<T> CreateObservableFromCallback<T>(CancellationToken cancelToken);
        ItemInstallInfo GetItemInstallInfo(PublishedFileId_t pid);
    }
}