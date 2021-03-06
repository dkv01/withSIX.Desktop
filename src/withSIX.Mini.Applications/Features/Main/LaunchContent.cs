﻿// <copyright company="SIX Networks GmbH" file="LaunchContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using withSIX.Api.Models.Extensions;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Features.Main.Games;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.GameLauncher;

namespace withSIX.Mini.Applications.Features.Main
{
    [ApiUserAction("Launch")]
    public class LaunchContent : SingleCntentBase, ICancellable, INotifyAction, IUseContent, IDisableDesktopNotification
    {
        public LaunchContent(Guid gameId, ContentGuidSpec content, LaunchType launchType = LaunchType.Default,
            LaunchAction action = LaunchAction.Default) : base(gameId, content) {
            LaunchType = launchType;
            Action = action;
        }

        public LaunchAction Action { get; }
        public LaunchType LaunchType { get; }
        public IPEndPoint ServerAddress { get; set; }
        public CancellationToken CancelToken { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public LaunchContentAction GetAction(Game game) {
            var content = game.Contents.FindContentOrThrow(Content.Id);
            var hasPath = content as IHavePath;
            return new LaunchContentAction(LaunchType, CancelToken,
                new ContentSpec(content, Content.Constraint)) {
                Name = Name,
                Href =
                    Href ??
                    (hasPath == null ? null : new Uri("http://withsix.com/p/" + game.GetContentPath(hasPath, Name))),
                Action = Action,
                ServerAddress = ServerAddress
            };
        }
    }

    public class LaunchContentValidator : AbstractValidator<LaunchContent>
    {
        public LaunchContentValidator() {
            RuleFor(c => c.ServerAddress).NotNull().When(c => c.Action == LaunchAction.Join);
        }
    }

    public class LaunchContentsValidator : AbstractValidator<LaunchContents>
    {
        public LaunchContentsValidator() {
            RuleFor(c => c.ServerAddress).NotNull().When(c => c.Action == LaunchAction.Join);
        }
    }

    [ApiUserAction("Launch")]
    public class LaunchContents : ContentsBase, ICancellable, INotifyAction, IUseContent, IDisableDesktopNotification
    {
        public LaunchContents(Guid gameId, List<ContentGuidSpec> contents, LaunchType launchType = LaunchType.Default,
            LaunchAction action = LaunchAction.Default) : base(gameId, contents) {
            LaunchType = launchType;
            Action = action;
        }

        public LaunchType LaunchType { get; }
        public LaunchAction Action { get; }
        public IPEndPoint ServerAddress { get; set; }
        public CancellationToken CancelToken { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public ILaunchContentAction<Content> GetAction(Game game) => new LaunchContentAction(
            Contents
                .DistinctBy(x => x.Id)
                .Select(x => new ContentSpec(game.Contents.FindContentOrThrow(x.Id), x.Constraint))
                .ToArray(), cancelToken: CancelToken) {
            Action = Action,
            Name = Name,
            Href = GetHref(game),
            ServerAddress = ServerAddress
        };
    }

    public class LaunchContentHandler : ApiDbCommandBase, IAsyncRequestHandler<LaunchContent>,
        IAsyncRequestHandler<LaunchContents>
    {
        readonly IGameLauncherFactory _factory;

        public LaunchContentHandler(IDbContextLocator gameContext, IGameLauncherFactory factory) : base(gameContext) {
            _factory = factory;
        }

        public async Task Handle(LaunchContent request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await game.Launch(_factory, request.GetAction(game)).ConfigureAwait(false);
        }

        public async Task Handle(LaunchContents request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await game.Launch(_factory, request.GetAction(game)).ConfigureAwait(false);
        }
    }
}