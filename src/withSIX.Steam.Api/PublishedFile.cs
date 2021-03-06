// <copyright company="SIX Networks GmbH" file="PublishedFile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using Steamworks;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Logging;
using withSIX.Steam.Api.Helpers;
using withSIX.Steam.Api.Services;

namespace withSIX.Steam.Api
{
    public class PublishedFile
    {
        public PublishedFile(ulong id, uint appId) : this(new PublishedFileId_t(id), new AppId_t(appId)) {}

        public PublishedFile(PublishedFileId_t id, AppId_t appId) {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (appId == null) throw new ArgumentNullException(nameof(appId));

            Pid = id;
            Aid = appId;
        }

        EItemState State => (EItemState) SteamUGC.GetItemState(Pid);
        public AppId_t Aid { get; }
        public PublishedFileId_t Pid { get; }

        public override string ToString() => Pid.ToString();

        public async Task Download(ISteamDownloader dl, ISteamApi api, Action<long?, double> progressAction = null,
            CancellationToken cancelToken = default(CancellationToken), bool force = false) {
            if (!force && !RequiresDownloading()) {
                progressAction?.Invoke(null, 100);
                return;
            }

            await HandleSubscription(force, api).ConfigureAwait(false);
            await dl.Download(this, progressAction, cancelToken).ConfigureAwait(false);
        }

        bool IsSubscribed() => State.IsSubscribed();
        bool IsInstalled() => State.IsInstalled();
        bool RequiresDownloading() => State.RequiresDownloading();
        bool IsLegacy() => State.IsLegacy();

        async Task HandleSubscription(bool force, ISteamApi api) {
            var isSubscribed = IsSubscribed();
            if (force && isSubscribed) {
                await api.UnsubscribeAndConfirm(Pid).ConfigureAwait(false);
                isSubscribed = false;
            }
            if (!isSubscribed)
                await api.SubscribeAndConfirm(Pid).ConfigureAwait(false);
        }

        public async Task<bool> Uninstall(ISteamApi api, IAbsoluteDirectoryPath workshopPath,
            CancellationToken cancelToken = default(CancellationToken)) {
            ItemInstallInfo info = null;
            if (IsInstalled()) {
                info = api.GetItemInstallInfo(Pid);
                MainLog.Logger.Debug($"IsInstalled! {info}");
            }

            var path = info?.GetLocation(IsLegacy()) ?? GetPath(workshopPath);
            if (IsSubscribed()) {
                await api.UnsubscribeAndConfirm(Pid).ConfigureAwait(false);
                // We delete the path anyway, because Steam will normally wait until the program exits otherwise
                // and since we're a tool and not the game, we don't have to wait!
                path.DeleteIfExists(true);
                return true;
            }
            path.DeleteIfExists(true);
            return false;
        }

        private IAbsolutePath GetPath(IAbsoluteDirectoryPath workshopPath)
            =>
            IsLegacy()
                ? (IAbsolutePath) workshopPath.GetChildFileWithName(Pid.ToString())
                : workshopPath.GetChildDirectoryWithName(Pid.ToString());
    }
}