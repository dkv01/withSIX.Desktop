// <copyright company="SIX Networks GmbH" file="App.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Steam.Api.Services;
using SN.withSIX.Steam.Api.SteamKit.Utils;
using SN.withSIX.Steam.Core;
using SN.withSIX.Steam.Core.Extensions;
using Steamworks;

namespace SN.withSIX.Steam.Api
{
    public class App
    {
        private readonly Lazy<SteamDirectories> _directories;
        private readonly ISteamApp _steamInfo;

        public App(uint id) : this(new AppId_t(id)) {}

        public App(AppId_t id) {
            Id = id;
            _steamInfo = SteamHelper.TryGetSteamAppById(Id.m_AppId);
            _directories = SystemExtensions.CreateLazy(GetDirectories);
        }

        // TODO: Move to SteamAPI...
        public static SteamHelper SteamHelper { get; set; }

        public AppId_t Id { get; set; }

        private SteamDirectories Directories => _directories.Value;

        SteamDirectories GetDirectories()
            => new SteamDirectories(Id.m_AppId, _steamInfo.GetInstallDir(), _steamInfo.InstallBase);

        public async Task Uninstall(PublishedFile pf, ISteamApi api,
            CancellationToken cancelToken = default(CancellationToken)) {
            if (!await pf.Uninstall(api, Directories.Workshop.ContentPath, cancelToken).ConfigureAwait(false))
                await HandleWorkshopItemMetadataRemoval(pf, cancelToken).ConfigureAwait(false);
        }

        public PublishedFile GetPf(ulong pId) => new PublishedFile(pId, Id.m_AppId);

        public async Task Download(ISteamDownloader steamDownloader, ISteamApi steamApi, PublishedFile pf,
            Action<long?, double> action, CancellationToken cancelToken = default(CancellationToken), bool force = false) {
            if (force)
                await HandleWorkshopItemMetadataRemoval(pf, cancelToken).ConfigureAwait(false);
            await pf.Download(steamDownloader, steamApi, action, cancelToken, force).ConfigureAwait(false);
        }

        // TODO: This probably actually needs Steam to close before performing actions again, as Steam caches the info and overwrites it again??
        private async Task HandleWorkshopItemMetadataRemoval(PublishedFile pf, CancellationToken cancelToken) {
            var wsf = $"appworkshop_{Id.m_AppId}.acf";
            var reg = Directories.Workshop.RootPath.GetChildFileWithName(wsf);
            MainLog.Logger.Info($"Considering rewriting {wsf}");
            if (!reg.Exists)
                return;

            var kv = await KeyValueHelper.LoadFromFileAsync(reg, cancelToken).ConfigureAwait(false);
            var changed = false;
            var key = pf.Pid.m_PublishedFileId.ToString();
            foreach (
                var root in
                new[] {
                    "WorkshopItemDetails",
                    "WorkshopItemsInstalled"
                }.Select(r => kv.GetKeyValue(r)).Where(root => root.ContainsKey(key))) {
                MainLog.Logger.Info($"Removing {key} from {root.Name}");
                root.Remove(key);
                changed = true;
            }

            if (changed) {
                MainLog.Logger.Info($"Changes detected, saving {wsf}");
                var id = kv["WorkshopItemsInstalled"];
                kv["SizeOnDisk"].Value = id.Children.Sum(x => x["size"].AsLong()).ToString();
                KeyValueHelper.SaveToFile(kv, reg);
            }
        }
    }
}