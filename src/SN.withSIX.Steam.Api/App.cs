// <copyright company="SIX Networks GmbH" file="App.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SN.withSIX.Steam.Api.Services;
using SN.withSIX.Steam.Core;
using SN.withSIX.Steam.Core.SteamKit.Utils;
using SteamKit2;
using Steamworks;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Steam.Api
{
    public class App
    {
        // TODO: Move to SteamAPI...
        internal static readonly SteamHelper SteamHelper = new SteamHelper(new SteamStuff().TryReadSteamConfig(),
            SteamStuff.GetSteamPath());
        private readonly SteamApp _steamInfo;
        public App(uint id) : this(new AppId_t(id)) {}

        public App(AppId_t id) {
            Id = id;
            _steamInfo = SteamHelper.TryGetSteamAppById(Id.m_AppId);
        }

        public AppId_t Id { get; set; }

        SteamDirectories GetDirectories()
            => new SteamDirectories(Id.m_AppId, _steamInfo.GetInstallDir(), _steamInfo.InstallBase);

        public async Task Uninstall(PublishedFile pf, ISteamApi api,
            CancellationToken cancelToken = default(CancellationToken)) {
            await pf.Uninstall(api, cancelToken).ConfigureAwait(false);
            await HandleWorkshopItemMetadataRemoval(pf, cancelToken).ConfigureAwait(false);
        }

        public PublishedFile GetPf(ulong pId) => new PublishedFile(pId, Id.m_AppId);

        public async Task Download(ISteamDownloader steamDownloader, ISteamApi steamApi, PublishedFile pf,
            Action<long?, double> action, CancellationToken cancelToken = default(CancellationToken), bool force = false) {
            if (force)
                await HandleWorkshopItemMetadataRemoval(pf, cancelToken).ConfigureAwait(false);
            await pf.Download(steamDownloader, steamApi, action, cancelToken, force).ConfigureAwait(false);
        }

        private async Task HandleWorkshopItemMetadataRemoval(PublishedFile pf, CancellationToken cancelToken) {
            var reg = GetDirectories().Workshop.RootPath.GetChildFileWithName($"appworkshop_{Id.m_AppId}.acf");
            if (reg.Exists) {
                var kv = KeyValue.LoadFromString(await reg.ReadTextAsync(cancelToken).ConfigureAwait(false));
                var changed = false;
                var key = pf.Pid.m_PublishedFileId.ToString();
                var ws = kv.GetKeyValue("AppWorkshop");
                foreach (
                    var root in
                        new[] {
                            "WorkshopItemDetails",
                            "WorkshopItemsInstalled"
                        }.Select(r => ws.GetKeyValue(r)).Where(root => root.ContainsKey(key))) {
                    root.Remove(key);
                    changed = true;
                }

                if (changed) {
                    var id = ws["WorkshopItemsInstalled"];
                    ws["SizeOnDisk"].Value = id.Children.Sum(x => x["size"].AsLong()).ToString();
                    kv.SaveToFile(reg.ToString(), false);
                }
            }
        }
    }
}