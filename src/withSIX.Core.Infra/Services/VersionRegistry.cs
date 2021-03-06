// <copyright company="SIX Networks GmbH" file="VersionRegistry.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Infrastructure;
using withSIX.Core.Applications.Services;
using withSIX.Core.Logging;

namespace withSIX.Core.Infra.Services
{
    public class VersionRegistry : IEnableLogging
    {
        const string VersionInfoFile = "VersionInfo.json";
        const string LocalVersionInfoFile = "LocalVersionInfo.json";
        readonly IApiLocalObjectCacheManager _cacheManager;
        readonly ISystemInfo _systemInfo;
        readonly Uri _versionInfoUrl;

        public VersionRegistry(Uri uri, IApiLocalObjectCacheManager cacheManager, ISystemInfo systemInfo) {
            _cacheManager = cacheManager;
            _systemInfo = systemInfo;

            _versionInfoUrl = Tools.Transfer.JoinUri(uri, VersionInfoFile);
            LocalVersionInfo = new VersionInfoDto();
            VersionInfo = new VersionInfoDto();
        }

        public VersionInfoDto LocalVersionInfo { get; private set; }
        public VersionInfoDto VersionInfo { get; private set; }

        public Task UpdateLocalVersion(Action<VersionInfoDto> action) {
            action(LocalVersionInfo);
            return Save(LocalVersionInfoFile, LocalVersionInfo);
        }

        async Task Save(string file, VersionInfoDto obj) {
            await _cacheManager.SetObject(file, obj.ToJson());
        }

        public async Task Init() {
            if (_systemInfo.IsInternetAvailable)
                await TryDownloadVersionInfo().ConfigureAwait(false);
            else
                await TryLoadVersionInfo().ConfigureAwait(false);
            await TryLoadLocalVersionInfo().ConfigureAwait(false);
        }

        async Task TryLoadLocalVersionInfo() {
            try {
                LocalVersionInfo = await GetToolsInfo(LocalVersionInfoFile).ConfigureAwait(false);
            } catch (KeyNotFoundException) {
                LocalVersionInfo = new VersionInfoDto();
            } catch (Exception) {
                LocalVersionInfo = new VersionInfoDto();
            }
        }

        async Task TryDownloadVersionInfo() {
            VersionInfoDto versionInfo;
            try {
                var dl = await _cacheManager.Download(_versionInfoUrl, TimeSpan.FromSeconds(30));
                versionInfo = Encoding.UTF8.GetString(dl).FromJson<VersionInfoDto>();
            } catch (Exception) {
                versionInfo = new VersionInfoDto();
            }
            await Save(VersionInfoFile, versionInfo).ConfigureAwait(false);
            VersionInfo = versionInfo;
        }

        async Task TryLoadVersionInfo() {
            try {
                VersionInfo = await GetToolsInfo(VersionInfoFile).ConfigureAwait(false);
            } catch (KeyNotFoundException) {
                VersionInfo = new VersionInfoDto();
            }
        }

        async Task<VersionInfoDto> GetToolsInfo(string versionInfoFile) {
            var json = await _cacheManager.GetObject<string>(versionInfoFile);
            return json.FromJson<VersionInfoDto>();
        }
    }
}