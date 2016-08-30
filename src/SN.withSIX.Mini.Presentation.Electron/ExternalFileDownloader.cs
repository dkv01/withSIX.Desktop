// <copyright company="SIX Networks GmbH" file="ExternalFileDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases.Main;

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

            // TODO: Support the external browser too, store reference ID to pick DL from, add a timeout?
            //if (Consts.PluginBrowserFound != Browser.None)
              //  Tools.Generic.OpenUrl(url);

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