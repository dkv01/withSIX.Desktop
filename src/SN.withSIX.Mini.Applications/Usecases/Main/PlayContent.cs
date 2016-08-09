// <copyright company="SIX Networks GmbH" file="PlayContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Attributes;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Applications.Usecases.Main.Games;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Mini.Core.Games.Services.GameLauncher;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    [ApiUserAction("Play")]
    public class PlayContent : SingleCntentBase, INeedCancellationTokenSource, INotifyAction, IUseContent, ICancelable
    {
        public PlayContent(Guid gameId, ContentGuidSpec content) : base(gameId, content) {}
        public DoneCancellationTokenSource CTS { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public PlayContentAction GetAction(Game game) {
            var content = game.Contents.FindContentOrThrow(Content.Id);
            var hasPath = content as IHavePath;
            var href = hasPath == null ? null : new Uri("http://withsix.com/p/" + game.GetContentPath(hasPath));
            return new PlayContentAction(cancelToken: CTS.Token,
                content: new ContentSpec(content, Content.Constraint)) {
                    Name = content.Name,
                    Href = href
                };
        }
    }

    [ApiUserAction("Play")]
    public class PlayContents : ContentsBase, INotifyAction, INeedCancellationTokenSource, IUseContent, ICancelable
    {
        public PlayContents(Guid gameId, List<ContentGuidSpec> contents) : base(gameId, contents) {}
        public DoneCancellationTokenSource CTS { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public PlayContentAction GetAction(Game game)
            => new PlayContentAction(
                Contents.Select(x => new ContentSpec(game.Contents.FindContentOrThrow(x.Id), x.Constraint))
                    .ToArray(), cancelToken: CTS.Token) {Name = Name, Href = GetHref(game)};
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