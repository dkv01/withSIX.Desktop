// <copyright company="SIX Networks GmbH" file="PublishedFile.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Steam.Api.Helpers;
using SN.withSIX.Steam.Api.Services;
using Steamworks;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Steam.Api
{
    public class PublishedFile
    {
        public PublishedFile(ulong id, uint appId) : this(new PublishedFileId_t(id), new AppId_t(appId)) {}

        public PublishedFile(PublishedFileId_t id, AppId_t appId) {
            Contract.Requires<ArgumentNullException>(id != null);
            Contract.Requires<ArgumentNullException>(appId != null);

            Pid = id;
            Aid = appId;
        }

        EItemState State => (EItemState) SteamUGC.GetItemState(Pid);
        public AppId_t Aid { get; }
        public PublishedFileId_t Pid { get; }

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

        public async Task Uninstall(ISteamApi api, CancellationToken cancelToken = default(CancellationToken)) {
            if (IsSubscribed())
                await api.UnsubscribeAndConfirm(Pid).ConfigureAwait(false);

            // TODO: What if Steam does not know the content is installed?
            if (IsInstalled()) {
                var info = api.GetItemInstallInfo(Pid);
                if (IsLegacy()) {
                    var f = info.Location.ToAbsoluteFilePath();
                    if (f.Exists)
                        f.Delete();
                } else {
                    var d = info.Location.ToAbsoluteDirectoryPath();
                    if (d.Exists)
                        d.Delete();
                }
                // TODO: Nuke the installation info from Steam
            }
        }
    }
}