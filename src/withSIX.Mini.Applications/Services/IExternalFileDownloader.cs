// <copyright company="SIX Networks GmbH" file="IExternalFileDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Core;
using withSIX.Mini.Applications.Usecases.Main;

namespace withSIX.Mini.Applications.Services
{
    public interface IExternalFileDownloader
    {
        Task<IAbsoluteFilePath> DownloadFile(Uri url, IAbsoluteDirectoryPath destination,
            Action<long?, double> progressAction, CancellationToken cancelToken = default(CancellationToken));

        Task StartSession(Uri url, IAbsoluteDirectoryPath destination,
            CancellationToken cancelToken = default(CancellationToken));

        bool RegisterExisting(Uri url, IAbsoluteFilePath path);
    }

    public abstract class ExternalFileDownloaderBase : IExternalFileDownloader
    {
        private readonly IDictionary<Uri, IAbsoluteFilePath> _cache = new Dictionary<Uri, IAbsoluteFilePath>();
        private readonly IDictionary<Uri, TaskCompletionSource<IAbsoluteFilePath>> _tasks =
            new Dictionary<Uri, TaskCompletionSource<IAbsoluteFilePath>>();

        protected ExternalFileDownloaderBase(IExternalDownloadStateHandler state) {
            State = state;
        }

        protected IExternalDownloadStateHandler State { get; }

        public async Task<IAbsoluteFilePath> DownloadFile(Uri url, IAbsoluteDirectoryPath destination,
            Action<long?, double> progressAction, CancellationToken token = default(CancellationToken)) {
            if (_cache.ContainsKey(url)) {
                var c = _cache[url];
                _cache.Remove(url);
                progressAction(100, 100);
                return c;
            }

            try {
                if (Consts.PluginBrowserFound != Browser.None)
                    return await HandleViaBrowser(url, token).ConfigureAwait(false);
                return await DownloadFileImpl(url, destination, progressAction, token).ConfigureAwait(false);
            } catch (Exception ex) {
                if (ex.Message.StartsWith("AbortedBeforeStartedError"))
                    throw new ExternalDownloadCancelled(ex.Message, ex);
                throw;
            }
        }


        public async Task StartSession(Uri url, IAbsoluteDirectoryPath destination,
            CancellationToken cancelToken = new CancellationToken()) {
            if (Consts.PluginBrowserFound != Browser.None) {
                Tools.Generic.OpenUrl(url);
                return;
            }
            await StartSessionImpl(url, destination, cancelToken).ConfigureAwait(false);
        }

        public bool RegisterExisting(Uri url, IAbsoluteFilePath path) {
            if (_tasks.ContainsKey(url)) {
                _tasks[url].SetResult(path);
                _tasks.Remove(url);
                return true;
            }
            _cache[url] = path;
            return false;
        }

        protected abstract Task<IAbsoluteFilePath> DownloadFileImpl(Uri url, IAbsoluteDirectoryPath destination,
            Action<long?, double> progressAction, CancellationToken token);

        protected abstract Task StartSessionImpl(Uri url, IAbsoluteDirectoryPath destination,
            CancellationToken cancelToken);

        protected Task<IAbsoluteFilePath> HandleViaBrowser(Uri url, CancellationToken token) {
            _tasks[url] = new TaskCompletionSource<IAbsoluteFilePath>();
            token.Register(_tasks[url].SetCanceled);
            Tools.Generic.OpenUrl(url);
            return _tasks[url].Task;
        }
    }

    public class ExternalDownloadCancelled : DidNotStartException
    {
        public ExternalDownloadCancelled(string message, Exception exception) : base(message, exception) {}
    }
}