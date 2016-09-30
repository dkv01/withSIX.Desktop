// <copyright company="SIX Networks GmbH" file="BrowserHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Core;
using withSIX.Play.Core.Connect.Events;
using withSIX.Play.Core.Games.Legacy;

namespace withSIX.Play.Core.Connect
{
    public static class BrowserHelper
    {
        public static bool TryOpenUrlIntegrated(string url) {
            if (String.IsNullOrWhiteSpace(url))
                return false;

            if (DomainEvilGlobal.Settings.AppOptions.PreferSystemBrowser)
                return Tools.Generic.TryOpenUrl(url);
            CalculatedGameSettings.RaiseEvent(new RequestOpenBrowser(new Uri(url)));
            return true;
        }

        public static bool TryOpenUrlIntegrated(Uri uri) {
            if (String.IsNullOrWhiteSpace(uri.ToString()))
                return false;

            if (DomainEvilGlobal.Settings.AppOptions.PreferSystemBrowser)
                return Tools.Generic.TryOpenUrl(uri);
            CalculatedGameSettings.RaiseEvent(new RequestOpenBrowser(uri));
            return true;
        }
    }
}