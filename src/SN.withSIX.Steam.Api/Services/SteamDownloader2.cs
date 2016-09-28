// <copyright company="SIX Networks GmbH" file="SteamDownloader2.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Mini.Infra.Api.Downloaders
{
    /*
    public class SteamDownloader : ISteamDownloader, IInfrastructureService
    {
        private readonly AsyncLock _l = new AsyncLock();

        public SteamDownloader() {
            var configFile = Common.Paths.LocalDataPath.GetChildFileWithName(@"DepotDownloader.config");
            ConfigStore.LoadFromFile(configFile.ToString());
        }

        public async Task Download(DownloadDetails info, IAbsoluteDirectoryPath location, LoginDetails login, Action<long?, double> action = null, CancellationToken ct = default(CancellationToken)) {
            using (await _l.LockAsync(ct).ConfigureAwait(false)) {
                var config = BuildConfig(info, location, action ?? ((l, f) => {}), ct);
                using (var dl = new ContentDownloader(config)) {
                    dl.InitializeSteam3(login.UserName, login.Password);
                    dl.DownloadApp(info.AppId, info.DepotId, "Public", true);
                }
            }
        }

        public async Task<Publishedfiledetail> GetPublishedInfo(ulong contentId) {
            var response =
                (await
                    GetPublishedFileDetailsMethod.Get(new GetPublishedFileDetailsRequest(contentId))
                        .ConfigureAwait(false)).response;
            if (response == null || response.resultcount == 0)
                throw new NotFoundException();
            return response.publishedfiledetails[0];
        }

        private static DownloadConfig BuildConfig(DownloadDetails info, IAbsoluteDirectoryPath location, Action<long?, double> progress, CancellationToken ct) {
            var config = new DownloadConfig {
                DownloadAllPlatforms = true,
                DownloadManifestOnly = false,
                //CellID = 20, // get from logon
                ManifestId = info.ManifestId,
                InstallDirectory = location,
                MaxServers = 8,
                MaxDownloads = 4,
                Progress = progress,
                CancelToken = ct
            };
            // ??
            config.MaxServers = Math.Max(config.MaxServers, config.MaxDownloads);
            return config;
        }
    }
    */
}