// <copyright company="SIX Networks GmbH" file="SteamUploader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using Steamworks;

namespace SN.withSIX.Steam.Api.Services
{
    public class SteamUploader
    {
        private void Bla(PublishedFile pf) {
            var s = UpdateAndConfirm(pf);
            ulong bytes;
            ulong bytesTotal;
            var p = SteamUGC.GetItemUpdateProgress(s, out bytes, out bytesTotal);
        }

        private static UGCUpdateHandle_t UpdateAndConfirm(PublishedFile pf) {
            var r = SteamUGC.StartItemUpdate(pf.Aid, pf.Pid);
            if (r == null)
                throw new InvalidOperationException("Failed to initiate update");
            return r;
        }
    }
}