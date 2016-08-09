// <copyright company="SIX Networks GmbH" file="LaunchContent.cs">
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
using SN.withSIX.Mini.Core.Games.Services.GameLauncher;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    [ApiUserAction("Launch")]
    public class LaunchContent : SingleCntentBase, INeedCancellationTokenSource, INotifyAction, IUseContent
    {
        public LaunchContent(Guid gameId, ContentGuidSpec content, LaunchType launchType = LaunchType.Default,
            LaunchAction action = LaunchAction.Default) : base(gameId, content) {
            LaunchType = launchType;
            Action = action;
        }

        public LaunchAction Action { get; }
        public LaunchType LaunchType { get; }
        public DoneCancellationTokenSource CTS { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public LaunchContentAction GetAction(Game game) {
            var content = game.Contents.FindContentOrThrow(Content.Id);
            var hasPath = content as IHavePath;
            var href = hasPath == null ? null : new Uri("http://withsix.com/p/" + game.GetContentPath(hasPath));
            return new LaunchContentAction(LaunchType, CTS.Token,
                new ContentSpec(content, Content.Constraint)) {Name = content.Name, Href = href, Action = Action};
        }
    }

    [ApiUserAction("Launch")]
    public class LaunchContents : ContentsBase, INeedCancellationTokenSource, INotifyAction, IUseContent
    {
        public LaunchContents(Guid gameId, List<ContentGuidSpec> contents, LaunchType launchType = LaunchType.Default,
            LaunchAction action = LaunchAction.Default) : base(gameId, contents) {
            LaunchType = launchType;
            Action = action;
        }

        public LaunchType LaunchType { get; }
        public LaunchAction Action { get; }
        public DoneCancellationTokenSource CTS { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public ILaunchContentAction<Content> GetAction(Game game) => new LaunchContentAction(
            Contents.Select(x => new ContentSpec(game.Contents.FindContentOrThrow(x.Id), x.Constraint))
                .ToArray(), cancelToken: CTS.Token) {
                    Action = Action,
                    Name = Name,
                    Href = GetHref(game)
                };
    }

    public class LaunchContentHandler : ApiDbCommandBase, IAsyncVoidCommandHandler<LaunchContent>,
        IAsyncVoidCommandHandler<LaunchContents>
    {
        readonly IGameLauncherFactory _factory;

        public LaunchContentHandler(IDbContextLocator gameContext, IGameLauncherFactory factory) : base(gameContext) {
            _factory = factory;
        }

        public async Task<Unit> Handle(LaunchContent request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await game.Launch(_factory, request.GetAction(game)).ConfigureAwait(false);
            return Unit.Value;
        }

        public async Task<Unit> Handle(LaunchContents request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await game.Launch(_factory, request.GetAction(game)).ConfigureAwait(false);
            return Unit.Value;
        }
    }
}