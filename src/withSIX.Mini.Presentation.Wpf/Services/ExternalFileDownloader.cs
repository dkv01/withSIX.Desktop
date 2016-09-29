// <copyright company="SIX Networks GmbH" file="ExternalFileDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Core.Presentation;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Usecases.Main;

namespace withSIX.Mini.Presentation.Wpf.Services
{
    public class ExternalFileDownloader : ExternalFileDownloaderBase, IPresentationService
    {
        public ExternalFileDownloader(IExternalDownloadStateHandler state) : base(state) {}

        protected override Task<IAbsoluteFilePath> DownloadFileImpl(Uri url, IAbsoluteDirectoryPath destination,
            Action<long?, double> progressAction, CancellationToken token) {
            throw new NotImplementedException();
        }

        protected override Task StartSessionImpl(Uri url, IAbsoluteDirectoryPath destination,
            CancellationToken cancelToken) {
            throw new NotImplementedException();
        }
    }
}