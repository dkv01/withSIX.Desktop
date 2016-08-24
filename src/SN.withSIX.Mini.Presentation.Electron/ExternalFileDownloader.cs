// <copyright company="SIX Networks GmbH" file="ExternalFileDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Applications.Services;

namespace SN.withSIX.Mini.Presentation.Electron
{
    public class ExternalFileDownloader : IExternalFileDownloader, IPresentationService
    {
        private readonly INodeApi _api;

        public ExternalFileDownloader(INodeApi api) {
            _api = api;
        }

        public async Task<IAbsoluteFilePath> DownloadFile(Uri url, IAbsoluteDirectoryPath destination,
            Action<long?, double> progressAction) {
            // TODO: Progress reporting..
            // TODO: cancellation
            var r = await _api.DownloadFile(url, destination.ToString()).ConfigureAwait(false);
            return r.ToAbsoluteFilePath();
        }
    }
}