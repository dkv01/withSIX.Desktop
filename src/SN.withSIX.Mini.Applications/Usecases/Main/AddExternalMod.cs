// <copyright company="SIX Networks GmbH" file="AddExternalMod.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Attributes;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class ExternalDownloadStarted : IAsyncCommand<Guid>
    {
        public Uri Referrer { get; set; }
        public uint Id { get; set; }
    }

    public class ExternalDownloadProgressing : IAsyncVoidCommand
    {
        public uint Id { get; set; }
        public uint BytesReceived { get; set; }
        public uint TotalBytes { get; set; }
    }

    public abstract class AddExternalMod : IAsyncVoidCommand, IHaveGameId, IHaveContentPublisher, IHaveRequestName
    {
        readonly Regex nexus = new Regex(@"https?://www.nexusmods.com/([^\/#]+)/mods/([^\/#]+)/");

        readonly Regex nmsm = new Regex(@"https?://nomansskymods.com/mods/([^\/#]+)");

        readonly Regex cf = new Regex(@"https?://community.playstarbound.com/resources/([^/]+)\.(\d+)");

        protected AddExternalMod(string fileName, Uri referrer) {
            FileName = fileName;
            Referrer = referrer;

            var r = Referrer.ToString();
            var m = nmsm.Match(r);
            if (m.Success) {
                GameId = GameGuids.NMS;
                Publisher = Publisher.NoMansSkyMods;
                PubId = m.Groups[1].Value;
                // TODO: Install as local mod when unknown etc
            } else {
                m = nexus.Match(r);
                if (m.Success) {
                    var g = m.Groups[1].Value;
                    if (g != "fallout4") {
                        Error = new ValidationException("The game is currently not supported: " + g);
                    }
                    GameId = GameGuids.Fallout4;
                    Publisher = Publisher.NexusMods;
                    PubId = m.Groups[2].Value;
                } else {
                    m = cf.Match(r);
                    if (m.Success) {
                        GameId = GameGuids.Starbound;
                        Publisher = Publisher.Chucklefish;
                        PubId = m.Groups[2].Value;
                    } else
                        Error = new ValidationException("The link is not recognized");
                }
            }
            if (Name == null)
                Name = PubId;
        }

        public Exception Error { get; set; }

        public string PubId { get; set; }

        public Publisher Publisher { get; }

        public Uri Referrer { get; }
        public string FileName { get; }

        public CancellationToken CancelToken { get; set; }

        public bool Force { get; set; }

        public bool HideLaunchAction { get; set; }

        public Guid ClientId { get; set; }
        public Guid RequestId { get; set; }
        public Guid GameId { get; }

        public ContentGuidSpec Content { get; set; }

        public DownloadContentAction GetAction(Game game) {
            var c =
                game.NetworkContent.FirstOrDefault(
                    x => x.Publishers.Any(p => p.Publisher == Publisher && p.PublisherId == PubId));
            if (c == null)
                throw new NotFoundException("Content not found");
            Content = new ContentGuidSpec(c.Id, null);
            var hasPath = c as IHavePath;
            return new DownloadContentAction(CancelToken,
                new InstallContentSpec(c, Content.Constraint)) {
                    HideLaunchAction = HideLaunchAction,
                    Force = Force,
                    Name = Name,
                    Href = Href ?? (hasPath == null ? null : new Uri("http://withsix.com/p/" + game.GetContentPath(hasPath, Name)))
                };
        }


        public string ActionTitleOverride => Force ? "Diagnose" : null;
        public string PauseTitleOverride => Force ? "Cancel" : null;

        public IAsyncVoidCommandBase GetNextAction()
            => new LaunchContent(GameId, Content);

        public string Name { get; set; }
        public Uri Href { get; set; }
    }

    public interface IHaveContentPublisher
    {
        string PubId { get; set; }
        Publisher Publisher { get; }
    }

    public class AddExternalModRead : AddExternalMod, IExcludeGameWriteLock
    {
        public AddExternalModRead(string fileName, Uri referrer, bool isSuccess = true) : base(fileName, referrer) {
            IsSuccess = isSuccess;
        }
        public bool IsSuccess { get; set; } // TODO: Use for abort
    }

    [ApiUserAction("Install")]
    public class AddExternalModWrite : AddExternalMod, IUseContent, INotifyAction, IHaveNexAction
    {

        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);
        public AddExternalModWrite(string fileName, Uri referrer) : base(fileName, referrer) {}
    }

    public class AddExternalModHandler : ApiDbCommandBase, IAsyncVoidCommandHandler<AddExternalModRead>, IAsyncVoidCommandHandler<AddExternalModWrite>, IAsyncRequestHandler<ExternalDownloadStarted, Guid>, IAsyncVoidCommandHandler<ExternalDownloadProgressing>
    {
        readonly IContentInstallationService _contentInstallation;
        private readonly IExternalFileDownloader _fd;
        private readonly IExternalDownloadStateHandler _state;

        public AddExternalModHandler(IDbContextLocator dbContextLocator,
            IContentInstallationService contentInstallation, IExternalFileDownloader fd, IExternalDownloadStateHandler state)
            : base(dbContextLocator) {
            _contentInstallation = contentInstallation;
            _fd = fd;
            _state = state;
        }

        public async Task<Unit> Handle(AddExternalModRead request) {
            // TODO
            if (request.Error != null)
                throw request.Error;
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            var action = request.GetAction(game);
            var content = action.Content.Select(x => x.Content).OfType<NetworkContent>().FirstOrDefault();
            if (content == null)
                await SendWrite(request).ConfigureAwait(false);
            else {
                content.OverrideSource(request.Publisher);
                if (!_fd.RegisterExisting(game.GetPublisherUrl(content), request.FileName.ToAbsoluteFilePath()))
                    await SendWrite(request).ConfigureAwait(false);
            }

            return Unit.Value;
        }

        private static Task<Unit> SendWrite(AddExternalModRead request)
            => Cheat.Mediator.SendAsync(new AddExternalModWrite(request.FileName, request.Referrer));

        public async Task<Unit> Handle(AddExternalModWrite request) {
            // This shouldnt happen because we already check it in Read..
            if (request.Error != null)
                throw request.Error;
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            var action = request.GetAction(game);

            var content = (IContentWithPackageName) action.Content.First().Content;
            content.OverrideSource(request.Publisher);
            //_fd.RegisterExisting(game.GetPublisherUrl(action.Content.Select(x => x.Content).OfType<NetworkContent>().First().Source),request.FileName.ToAbsoluteFilePath());
            await game.Install(_contentInstallation, action).ConfigureAwait(false);

            return Unit.Value;
        }

        public async Task<Guid> Handle(ExternalDownloadStarted message) {
            await _state.UpdateState(message.Id, 0, 0).ConfigureAwait(false);
            return Guid.Empty;
        }

        public async Task<Unit> Handle(ExternalDownloadProgressing message) {
            await _state.UpdateState(message.Id, message.BytesReceived, message.TotalBytes).ConfigureAwait(false);

            return Unit.Value;
        }

        public class ExternalDownloadState
        {
            public Dictionary<uint, ProgressInfo> Progress = new Dictionary<uint, ProgressInfo>();
        }
    }

    public interface IExternalDownloadStateHandler {
        Tuple<uint, uint> Current { get; }
        Task UpdateState(uint id, uint bytesReceived, uint totalBytes);
    }

    public class ExternalDownloadStateHandler : IApplicationService, IExternalDownloadStateHandler
    {
        IDictionary<uint, Tuple<uint, uint>> storage = new Dictionary<uint, Tuple<uint, uint>>();

        public Tuple<uint, uint> Current { get; private set; }

        public void Clear() {
            Current = null;
            storage = new Dictionary<uint, Tuple<uint, uint>>();
        }

        public async Task UpdateState(uint id, uint bytesReceived, uint totalBytes) {
            Current = storage[id] = Tuple.Create(bytesReceived, totalBytes);
        }
    }
}