// <copyright company="SIX Networks GmbH" file="UploadFolder.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using withSIX.Api.Models;
using withSIX.Api.Models.Exceptions;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Helpers;
using withSIX.Core.Logging;
using withSIX.Core.Services.Infrastructure;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core;
using withSIX.Mini.Core.Games;
using withSIX.Sync.Core.Transfer;
using withSIX.Sync.Core.Transfer.Protocols.Handlers;

namespace withSIX.Mini.Applications.Features.Main
{
    /*
var client = w6Cheat.container.get(w6Cheat.containerObjects.client);
client.prepareFolder()
  .then(folder => client.uploadFolder({folder: folder, gameId: "9DE199E3-7342-4495-AD18-195CF264BA5B", contentId: "64de04c4-6751-4a3a-b915-7f3b1467f93f"}))
  .then(x => console.log("Complete!"))
  .catch(x => console.error("ERROR: ", x));
*/

    // TODO:
    // - Do not allow uploading while repository is in processing state
    // - Option to sign and process userconfigs on client??
    // - Verify structure
    // - Get authorization key and userId from account for this modfolder
    // - Once completed, signal the API for processing, the website can signal this??
    //   - Create synq package, update api etc.
    [ApiUserAction]
    public class UploadFolder : ICommand<Guid>, IHaveGameId, IExcludeGameWriteLock
    {
        public UploadFolder(string folder, Guid userId, Guid gameId, Guid contentId) {
            Folder = folder;
            UserId = userId;
            GameId = gameId;
            ContentId = contentId;
        }

        public string Folder { get; }
        public Guid UserId { get; }
        public Guid ContentId { get; }

        public string UserName { get; set; }
        public string Password { get; set; }
        public Guid GameId { get; }
    }

    public class UploadFolderHandler : DbCommandBase, IAsyncRequestHandler<UploadFolder, Guid>
    {
        private readonly IW6Api _api;
        readonly IFolderHandler _folderHandler;
        private readonly IQueueManager _queueManager;
        private readonly IRsyncLauncher _rsyncLauncher;

        public UploadFolderHandler(IDbContextLocator dbContextLocator, IFolderHandler folderHandler,
            IQueueManager queueManager, IRsyncLauncher rsyncLauncher, IW6Api api)
            : base(dbContextLocator) {
            _folderHandler = folderHandler;
            _queueManager = queueManager;
            _rsyncLauncher = rsyncLauncher;
            _api = api;
        }

        public async Task<Guid> Handle(UploadFolder request) {
            var path = request.Folder.ToAbsoluteDirectoryPath();
            if (!await _folderHandler.IsFolderWhitelisted(path).ConfigureAwait(false))
                throw new ValidationException("This folder was not yet whitelisted!");

            await UpdateOrAddContentInfo(request, path).ConfigureAwait(false);
            // intermediate save..
            await ContentLinkContext.SaveChanges().ConfigureAwait(false);

            var id = await
                _queueManager.AddToQueue("Upload " + path.DirectoryName, request.ContentId,
                    (progress, ct) => UploadAndProgressHandling(request, ct, progress)).ConfigureAwait(false);

            // Not retry compatible atm, also this is more a workaround for the upload stuff
            var item = _queueManager.Queue.Items.First(x => x.Id == id);
            await item.Task.ConfigureAwait(false);

            return id;
        }

        private async Task UploadAndProgressHandling(UploadFolder request, CancellationToken ct,
            Action<ProgressState> progress) {
            using (var sub = new Subject<ProgressState>())
            using (sub
                .Sample(TimeSpan.FromSeconds(5))
                .ConcatTask(s => TryUpdateProgress(request, s)).Subscribe()) {
                await UploadFolder(request, ct, s => {
                    sub.OnNext(s);
                    progress(s);
                }).ConfigureAwait(false);
            }
        }

        private async Task TryUpdateProgress(UploadFolder request, ProgressState s) {
            try {
                await UpdateProgress(s, request.ContentId, request.GameId).ConfigureAwait(false);
            } catch (Exception ex) {
                MainLog.Logger.FormattedWarnException(ex);
            }
        }

        private Task UpdateProgress(ProgressState progress, Guid requestContentId, Guid requestGameId)
            => _api.PostUploadProgress(requestContentId,
                new ProgressStateInfo {Progress = progress.Progress, Speed = progress.Speed});

        private async Task UpdateOrAddContentInfo(UploadFolder request, IAbsoluteDirectoryPath path) {
            var cl = await ContentLinkContext.GetFolderLink().ConfigureAwait(false);
            var l = cl.Infos.FirstOrDefault(x => x.Path.Equals(path));
            var contentInfo = new ContentInfo(request.UserId, request.GameId, request.ContentId);
            if (l != null)
                l.ContentInfo = contentInfo;
            else
                cl.Infos.Add(new FolderInfo(path, contentInfo));
        }

        // TODO: Split to service
        async Task UploadFolder(UploadFolder request, CancellationToken token, Action<ProgressState> progress) {
            var auth = new AuthInfo(request.UserName, request.Password);

            const string host = "staging.sixmirror.com";
            var folderPath =
                $"{new ShortGuid(request.UserId)}/{new ShortGuid(request.GameId)}/{new ShortGuid(request.ContentId)}";
            Environment.SetEnvironmentVariable("RSYNC_PASSWORD", auth.Password);

            var tp = new TransferProgress();
            using (new Monitor(tp, progress)) {
                var result = await
                    _rsyncLauncher.RunAndProcessAsync(tp,
                            request.Folder + "\\.", // "."
                            $"rsync://{auth.UserName}@{host}/{folderPath}", token,
                            new RsyncOptions {
                                AdditionalArguments = {
                                    "-avz",
                                    "--exclude=.rsync --exclude=.svn --exclude=.git --exclude=.hg --exclude=.synqinfo"
                                }
                            })
                        // , WorkingDirectory = request.Folder
                        .ConfigureAwait(false);
                MainLog.Logger.Debug("Output" + result.StandardOutput + "\nError " + result.StandardError);
                result.ConfirmSuccess();
            }
        }

        class Monitor : IDisposable
        {
            private readonly Action<ProgressState> _action;
            private readonly ITransferProgress _progress;
            private TimerWithoutOverlap _timer;

            public Monitor(ITransferProgress progress, Action<ProgressState> action) {
                _progress = progress;
                _action = action;
                _timer = new TimerWithoutOverlap(TimeSpan.FromMilliseconds(500), OnElapsed);
            }

            public void Dispose() {
                _timer.Dispose();
                _timer = null;
            }

            private void OnElapsed() {
                _action(new ProgressState(_progress.Progress, _progress.Speed, "Uploading"));
            }
        }

        class AuthInfo
        {
            public AuthInfo(string authInfo) {
                var authSplit = authInfo.Split(':');
                UserName = authSplit[0];
                Password = authSplit[1];
            }

            public AuthInfo(string userName, string password) {
                UserName = userName;
                Password = password;
            }

            public string UserName { get; }
            public string Password { get; }
        }
    }
}