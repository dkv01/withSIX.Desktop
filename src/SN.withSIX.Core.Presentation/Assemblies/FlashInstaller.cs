﻿// <copyright company="SIX Networks GmbH" file="FlashInstaller.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Win32;
using SN.withSIX.Core.Extensions;

namespace SN.withSIX.Core.Presentation.Assemblies
{
    public class FlashInstaller
    {
        readonly Uri _downloadUri;
        readonly string _tmpFile;
        readonly string _tmpLocation;

        public FlashInstaller(string tmpLocation, Uri downloadUri) {
            _tmpLocation = tmpLocation;
            _downloadUri = downloadUri;
            _tmpFile = Path.Combine(tmpLocation, Path.GetFileName(downloadUri.AbsolutePath));
        }

        public bool IsInstalled() {
            var registryKey = GetKey();
            if (registryKey == null)
                return false;

            var path = registryKey.GetValue("PlayerPath") as string;
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        static RegistryKey GetKey() => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
            .OpenSubKey(
                @"SOFTWARE\Macromedia\FlashPlayerPepper");

        public async Task Install() {
            using (new TmpFile(_tmpFile)) {
                await Download().ConfigureAwait(false);
                await RunInstaller().ConfigureAwait(false);
            }
        }

        Task RunInstaller() => TaskExtExt.StartLongRunningTask(() => {
            using (var p = Process.Start(_tmpFile, "-install")) {
                //  /vREINSTALL=ALL /vREINSTALLMODE=vomus /v/qb
                p.WaitForExit();
            }
            if (!IsInstalled())
                throw new InstallationFailed();
        });

        async Task Download() {
            Directory.CreateDirectory(_tmpLocation);
            using (var wc = new WebClient())
                await wc.DownloadFileTaskAsync(_downloadUri, _tmpFile).ConfigureAwait(false);
        }
    }
}