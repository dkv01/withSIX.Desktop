// <copyright company="SIX Networks GmbH" file="RequestOpenBrowser.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;

namespace withSIX.Play.Core.Connect.Events
{
    public class RequestOpenBrowser : EventArgs
    {
        public RequestOpenBrowser(Uri url) {
            if (url == null) throw new ArgumentNullException(nameof(url));

            Url = url;
        }

        public RequestOpenBrowser(string url) {
            if (url == null) throw new ArgumentNullException(nameof(url));

            Url = new Uri(url);
        }

        public Uri Url { get; set; }
    }


    public class RequestOpenLogin {}
}