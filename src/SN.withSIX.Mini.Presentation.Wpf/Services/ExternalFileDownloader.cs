// <copyright company="SIX Networks GmbH" file="ExternalFileDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases.Main;

namespace SN.withSIX.Mini.Presentation.Wpf.Services
{
    public class ExternalFileDownloader : IExternalFileDownloader, IPresentationService
    {
        private IDictionary<Uri, IAbsoluteFilePath> cache = new Dictionary<Uri, IAbsoluteFilePath>();
        private IDictionary<Uri, TaskCompletionSource<IAbsoluteFilePath>> tasks = new Dictionary<Uri, TaskCompletionSource<IAbsoluteFilePath>>();
        public async Task<IAbsoluteFilePath> DownloadFile(Uri url, IAbsoluteDirectoryPath destination,
            Action<long?, double> progressAction, CancellationToken token = default(CancellationToken)) {
            if (cache.ContainsKey(url)) {
                var c = cache[url];
                cache.Remove(url);
                return c;
            }

            if (Consts.PluginBrowserFound != Browser.None)
                return await HandleViaBrowser(url, token).ConfigureAwait(false);
            throw new NotImplementedException();
        }

        public async Task StartSession(Uri url, IAbsoluteDirectoryPath destination, CancellationToken cancelToken = new CancellationToken()) {
            if (Consts.PluginBrowserFound != Browser.None) {
                Tools.Generic.OpenUrl(url);
                return;
            }
            throw new NotImplementedException();
        }

        private async Task<IAbsoluteFilePath> HandleViaBrowser(Uri url, CancellationToken token) {
            tasks[url] = new TaskCompletionSource<IAbsoluteFilePath>();
            token.Register(tasks[url].SetCanceled);
            Tools.Generic.OpenUrl(url);
            try {
                return await tasks[url].Task;
            } finally {
                if (tasks.ContainsKey(url))
                    tasks.Remove(url);
            }
        }

        public bool RegisterExisting(Uri url, IAbsoluteFilePath path) {
            if (tasks.ContainsKey(url)) {
                tasks[url].SetResult(path);
                tasks.Remove(url);
                return true;
            }
            cache[url] = path;
            return false;
        }
    }
}