// <copyright company="SIX Networks GmbH" file="ISteamDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Mini.Applications.Services
{
    /*
    public interface ISteamDownloader
    {
        Task Download(DownloadDetails info, IAbsoluteDirectoryPath location, LoginDetails login, Action<long?, double> action = null, CancellationToken ct = default(CancellationToken));
        Task<Publishedfiledetail> GetPublishedInfo(ulong contentId);
    }

    public class LoginDetails
    {
        public LoginDetails(string userName, string password) {
            UserName = userName;
            Password = password;
        }

        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class DownloadDetails
    {
        public DownloadDetails(ulong manifestId, uint appId) {
            ManifestId = manifestId;
            AppId = appId;
            DepotId = AppId;
        }

        public ulong ManifestId { get; set; }
        public uint AppId { get; set; }
        public uint DepotId { get; set; }
    }

    public static class SteamDownloaderExtensions
    {
        public static async Task Download(this ISteamDownloader downloader, ulong contentId, IAbsoluteDirectoryPath location, LoginDetails login, Action<long?, double> action, CancellationToken ct = default(CancellationToken)) {
            var info = await downloader.GetPublishedInfo(contentId).ConfigureAwait(false);
            if (info.result == EResult.FileNotFound)
                throw new NotFoundException($"The content with ID {contentId} was not (or no longer) found");

            await
                downloader.Download(new DownloadDetails(ulong.Parse(info.hcontent_file), info.consumer_app_id), location,
                    login, action, ct).ConfigureAwait(false);
        }
    }
    */
}