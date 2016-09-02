// <copyright company="SIX Networks GmbH" file="ExternalFileDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Applications.Services;

namespace SN.withSIX.Mini.Presentation.Wpf.Services
{
    public class ExternalFileDownloader : ExternalFileDownloaderBase, IPresentationService
    {
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