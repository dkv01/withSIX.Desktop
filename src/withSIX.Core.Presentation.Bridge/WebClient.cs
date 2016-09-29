// <copyright company="SIX Networks GmbH" file="WebClient.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Net;
using withSIX.Core.Helpers;
using withSIX.Sync.Core.Transfer;

namespace withSIX.Core.Presentation.Bridge
{
    public class WebClient : System.Net.WebClient, IWebClient, ITransientService
    {
        public WebClient() : this(100*1000) {}

        protected WebClient(int timeout) {
            Timeout = timeout;
        }

        public int Timeout { get; set; }

        public IDisposable SetupDownloadTransferProgress(ITransferProgress transferProgress, TimeSpan timeout) {
            var lastTime = Tools.Generic.GetCurrentUtcDateTime;
            long lastBytes = 0;

            transferProgress.Update(null, 0);
            transferProgress.FileSizeTransfered = 0;
            DownloadProgressChanged +=
                (sender, args) => {
                    var bytes = args.BytesReceived;
                    var now = Tools.Generic.GetCurrentUtcDateTime;

                    transferProgress.FileSizeTransfered = bytes;

                    long? speed = null;

                    if (lastBytes != 0) {
                        var timeSpan = now - lastTime;
                        var bytesChange = bytes - lastBytes;

                        if (timeSpan.TotalMilliseconds > 0)
                            speed = (long) (bytesChange/(timeSpan.TotalMilliseconds/1000.0));
                    }
                    transferProgress.Update(speed, args.ProgressPercentage);

                    lastBytes = bytes;
                    lastTime = now;
                };

            DownloadFileCompleted += (sender, args) => { transferProgress.Completed = true; };

            var timer = new TimerWithElapsedCancellation(500, () => {
                if (!transferProgress.Completed
                    && Tools.Generic.LongerAgoThan(lastTime, timeout)) {
                    CancelAsync();
                    return false;
                }
                return !transferProgress.Completed;
            });
            return timer;
        }


        public IDisposable SetupUploadTransferProgress(ITransferProgress transferProgress, TimeSpan timeout) {
            var lastTime = Tools.Generic.GetCurrentUtcDateTime;
            long lastBytes = 0;

            UploadProgressChanged +=
                (sender, args) => {
                    var bytes = args.BytesReceived;
                    var now = Tools.Generic.GetCurrentUtcDateTime;

                    transferProgress.FileSizeTransfered = bytes;
                    long? speed = null;
                    if (lastBytes != 0) {
                        var timeSpan = now - lastTime;
                        var bytesChange = bytes - lastBytes;

                        if (timeSpan.TotalMilliseconds > 0)
                            speed = (long) (bytesChange/(timeSpan.TotalMilliseconds/1000.0));
                    }
                    transferProgress.Update(speed, args.ProgressPercentage);
                    lastTime = now;
                    lastBytes = bytes;
                };

            UploadFileCompleted += (sender, args) => { transferProgress.Completed = true; };

            var timer = new TimerWithElapsedCancellation(500, () => {
                if (!transferProgress.Completed
                    && Tools.Generic.LongerAgoThan(lastTime, timeout)) {
                    CancelAsync();
                    return false;
                }
                return !transferProgress.Completed;
            });
            return timer;
        }

        public void SetAuthInfo(string userInfo) {
            var splitUserInfo = userInfo.Split(':');
            if (splitUserInfo.Length < 2)
                return;
            Credentials = new NetworkCredential(splitUserInfo[0], splitUserInfo[1]);
        }

        public void SetAuthInfo(Uri uri) {
            SetAuthInfo(uri.UserInfo);
        }

        protected override WebRequest GetWebRequest(Uri address) {
            var request = base.GetWebRequest(address);
            SetHttpKeepAlive(request);
            SetSynchronousTimeout(request);
            return request;
        }

        static void SetHttpKeepAlive(WebRequest request) {
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
                httpRequest.KeepAlive = false;
        }

        void SetSynchronousTimeout(WebRequest request) {
            if (request != null)
                request.Timeout = Timeout;
        }
    }
}