// <copyright company="SIX Networks GmbH" file="ExternalFileDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Applications.Services;

namespace SN.withSIX.Mini.Presentation.Electron
{
    public class ExternalFileDownloader : ExternalFileDownloaderBase, IPresentationService
    {
        private readonly INodeApi _api;

        public ExternalFileDownloader(INodeApi api) {
            _api = api;
        }

        // TODO: Progress reporting
        protected override Task<IAbsoluteFilePath> DownloadFileImpl(Uri url, IAbsoluteDirectoryPath destination,
            Action<long?, double> progressAction, CancellationToken token)
            => _api.DownloadFile(url, destination.ToString(), token);

        protected override Task StartSessionImpl(Uri url, IAbsoluteDirectoryPath destination,
            CancellationToken cancelToken) => _api.DownloadSession(url, destination.ToString(), cancelToken);
    }
}