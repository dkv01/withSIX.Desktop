// <copyright company="SIX Networks GmbH" file="FileDownloaderExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Sync.Core.Transfer.Specs;

namespace withSIX.Sync.Core.Transfer
{
    public static class FileDownloaderExtensions
    {
        public static void Download(this IFileDownloader downloader, string url, IAbsoluteFilePath file) {
            downloader.Download(new FileDownloadSpec(url, file));
        }

        public static void Download(this IFileDownloader downloader, Uri uri, IAbsoluteFilePath file) {
            downloader.Download(new FileDownloadSpec(uri, file));
        }

        public static void Download(this IFileDownloader downloader, string url, IAbsoluteFilePath file,
            ITransferProgress transferProgress) {
            downloader.Download(new FileDownloadSpec(url, file, transferProgress));
        }

        public static void Download(this IFileDownloader downloader, Uri uri, IAbsoluteFilePath file,
            ITransferProgress transferProgress) {
            downloader.Download(new FileDownloadSpec(uri, file, transferProgress));
        }

        public static Task DownloadAsync(this IFileDownloader downloader, string url, IAbsoluteFilePath file)
            => downloader.DownloadAsync(new FileDownloadSpec(url, file));

        public static Task DownloadAsync(this IFileDownloader downloader, Uri uri, IAbsoluteFilePath file)
            => downloader.DownloadAsync(new FileDownloadSpec(uri, file));

        public static Task DownloadAsync(this IFileDownloader downloader, string url, IAbsoluteFilePath file,
                ITransferProgress transferProgress)
            => downloader.DownloadAsync(new FileDownloadSpec(url, file, transferProgress));

        public static Task DownloadAsync(this IFileDownloader downloader, Uri uri, IAbsoluteFilePath file,
                ITransferProgress transferProgress)
            => downloader.DownloadAsync(new FileDownloadSpec(uri, file, transferProgress));
    }
}