﻿// <copyright company="SIX Networks GmbH" file="DownloadConfig.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using NDepend.Path;

namespace withSIX.Steam.Api.SteamKit.DepotDownloader
{
    public class DownloadConfig
    {
        public uint CellID { get; set; }
        public bool DownloadAllPlatforms { get; set; }
        public bool DownloadManifestOnly { get; set; }
        public IAbsoluteDirectoryPath InstallDirectory { get; set; }

        public bool UsingFileList { get; set; }
        public List<string> FilesToDownload { get; set; }
        public List<Regex> FilesToDownloadRegex { get; set; }

        public bool UsingExclusionList { get; set; }

        public string BetaPassword { get; set; }

        public ulong ManifestId { get; set; }

        public bool VerifyAll { get; set; }

        public int MaxServers { get; set; }
        public int MaxDownloads { get; set; }
        public Action<long?, double> Progress { get; set; } = (l, l1) => { };
        public CancellationToken CancelToken { get; set; }
    }
}