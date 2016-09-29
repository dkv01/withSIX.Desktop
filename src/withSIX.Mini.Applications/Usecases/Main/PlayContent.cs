// <copyright company="SIX Networks GmbH" file="PlayContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Applications.Usecases.Main.Games;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Mini.Core.Games.Services.GameLauncher;

namespace withSIX.Mini.Applications.Usecases.Main
{
    [ApiUserAction("Play")]
    public class PlayContent : SingleCntentBase, ICancellable, INotifyAction, IUseContent, ICancelable
    {
        public PlayContent(Guid gameId, ContentGuidSpec content) : base(gameId, content) {}
        public CancellationToken CancelToken { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public PlayContentAction GetAction(Game game) {
            var content = game.Contents.FindContentOrThrow(Content.Id);
            var hasPath = content as IHavePath;
            return new PlayContentAction(cancelToken: CancelToken,
                content: new ContentSpec(content, Content.Constraint)) {
                Name = Name,
                Href =
                    Href ??
                    (hasPath == null ? null : new Uri("http://withsix.com/p/" + game.GetContentPath(hasPath, Name)))
            };
        }
    }

    [ApiUserAction("Play")]
    public class PlayContents : ContentsBase, INotifyAction, ICancellable, IUseContent, ICancelable
    {
        public PlayContents(Guid gameId, List<ContentGuidSpec> contents) : base(gameId, contents) {}
        public CancellationToken CancelToken { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public PlayContentAction GetAction(Game game)
            => new PlayContentAction(
                Contents.Select(x => new ContentSpec(game.Contents.FindContentOrThrow(x.Id), x.Constraint))
                    .ToArray(), cancelToken: CancelToken) {Name = Name, Href = GetHref(game)};
    }


    public class PlayContentHandler : ApiDbCommandBase, IAsyncVoidCommandHandler<PlayContent>,
        IAsyncVoidCommandHandler<PlayContents>
    {
        readonly IContentInstallationService _contentInstallation;
        readonly IGameLauncherFactory _factory;

        public PlayContentHandler(IDbContextLocator gameContext, IGameLauncherFactory factory,
            IContentInstallationService contentInstallation) : base(gameContext) {
            _factory = factory;
            _contentInstallation = contentInstallation;
        }

        public async Task<Unit> Handle(PlayContent request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await game.Play(_factory, _contentInstallation, request.GetAction(game)).ConfigureAwait(false);
            return Unit.Value;
        }

        public async Task<Unit> Handle(PlayContents request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await game.Play(_factory, _contentInstallation, request.GetAction(game)).ConfigureAwait(false);
            return Unit.Value;
        }
    }
}