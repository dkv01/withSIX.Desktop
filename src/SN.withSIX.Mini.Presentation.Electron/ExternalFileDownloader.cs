// <copyright company="SIX Networks GmbH" file="ExternalFileDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Applications.Services;

namespace SN.withSIX.Mini.Presentation.Electron
{
    public class ExternalFileDownloader : IExternalFileDownloader, IPresentationService
    {
        private readonly INodeApi _api;
        private IDictionary<Uri, IAbsoluteFilePath> cache = new Dictionary<Uri, IAbsoluteFilePath>();

        public ExternalFileDownloader(INodeApi api) {
            _api = api;
        }

        public async Task<IAbsoluteFilePath> DownloadFile(Uri url, IAbsoluteDirectoryPath destination,
            Action<long?, double> progressAction) {
            if (cache.ContainsKey(url)) {
                var c = cache[url];
                cache.Remove(url);
                return c;
            }
            // TODO: Progress reporting..
            // TODO: cancellation
            var r = await _api.DownloadFile(url, destination.ToString()).ConfigureAwait(false);
            return r.ToAbsoluteFilePath();
        }

        public void RegisterExisting(Uri url, IAbsoluteFilePath path) {
            cache[url] = path;
        }
    }
}