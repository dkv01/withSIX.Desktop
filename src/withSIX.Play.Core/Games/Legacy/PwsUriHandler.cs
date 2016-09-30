// <copyright company="SIX Networks GmbH" file="PwsUriHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Core.Extensions;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Sync.Core.Transfer;

namespace withSIX.Play.Core.Games.Legacy
{
    public static class PwsUriHandler
    {
        public static Func<Uri, AuthInfo> GetAuthInfoFromUri;
        public static Action<Uri, AuthInfo> SetAuthInfo;

        public static Uri GetAuthlessUri(this Uri uri) {
            var authlessUri = uri.AuthlessUri();

            StoreUrlAuthInfo(uri, authlessUri);

            return authlessUri;
        }

        static void StoreUrlAuthInfo(Uri uri, Uri authlessUri) {
            var authInfo = GetAuthInfoFromUri(uri);
            if (authInfo.Username != null
                || authInfo.Password != null
                || authInfo.Domain != null)
                SetAuthInfo(authlessUri, authInfo);
        }

        public static Uri GetCleanedAuthlessUrl(this Uri uri) => uri.GetAuthlessUri().GetCleanuri();
    }
}