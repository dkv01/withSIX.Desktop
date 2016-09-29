// <copyright company="SIX Networks GmbH" file="ExternalFileDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Core.Presentation;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Usecases.Main;

namespace withSIX.Mini.Presentation.Electron
{
    public class ExternalFileDownloader : ExternalFileDownloaderBase, IPresentationService
    {
        private readonly INodeApi _api;

        public ExternalFileDownloader(INodeApi api, IExternalDownloadStateHandler state) : base(state) {
            _api = api;
        }

        // TODO: Progress reporting
        protected override async Task<IAbsoluteFilePath> DownloadFileImpl(Uri url, IAbsoluteDirectoryPath destination,
            Action<long?, double> progressAction, CancellationToken token) {
            var lastTime = DateTime.UtcNow;
            uint lastBytes = 0;
            State.Clear();
            using (Observable.Interval(TimeSpan.FromMilliseconds(500)).Select(x => State.Current).Where(x => x != null)
                .Where(x => x.Item2 > 0)
                .Do(x => {
                    long? speed = null;
                    if (lastBytes != 0) {
                        var timeSpan = DateTime.UtcNow - lastTime;
                        if (timeSpan.TotalMilliseconds > 0) {
                            var bytesChange = x.Item1 - lastBytes;
                            speed = (long) (bytesChange/(timeSpan.TotalMilliseconds/1000.0));
                        }
                    }
                    progressAction(speed, x.Item1/(double) x.Item2*100);
                    lastTime = DateTime.UtcNow;
                    lastBytes = x.Item1;
                }).Subscribe())
                return await _api.DownloadFile(url, destination.ToString(), token).ConfigureAwait(false);
        }

        protected override Task StartSessionImpl(Uri url, IAbsoluteDirectoryPath destination,
            CancellationToken cancelToken) => _api.DownloadSession(url, destination.ToString(), cancelToken);
    }
}