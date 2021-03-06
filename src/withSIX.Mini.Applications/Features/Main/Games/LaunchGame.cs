// <copyright company="SIX Networks GmbH" file="LaunchGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.GameLauncher;

namespace withSIX.Mini.Applications.Features.Main.Games
{
    [ApiUserAction("Launch")]
    public class LaunchGame : RequestBase, IHaveId<Guid>, ICancellable, INotifyAction
    {
        public LaunchGame(Guid id, LaunchType launchType) {
            Id = id;
            LaunchType = launchType;
        }

        public LaunchType LaunchType { get; }

        public LaunchAction Action { get; set; }

        public IPEndPoint ServerAddress { get; set; }
        public CancellationToken CancelToken { get; set; }
        public Guid Id { get; }
        public Guid GameId => Id;

        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public ILaunchContentAction<Content> GetAction(Game game)
            => new LaunchContentAction(LaunchType, CancelToken) {
                Action = Action,
                Name = game.Metadata.Name,
                ServerAddress = ServerAddress
            };
    }

    public class LaunchGameValidator : AbstractValidator<LaunchGame>
    {
        public LaunchGameValidator() {
            RuleFor(c => c.ServerAddress).NotNull().When(c => c.Action == LaunchAction.Join);
        }
    }

    public class LaunchGameHandler : DbCommandBase, IAsyncRequestHandler<LaunchGame>
    {
        readonly IGameLauncherFactory _launcherFactory;

        public LaunchGameHandler(IDbContextLocator dbContextLocator, IGameLauncherFactory launcherFactory)
            : base(dbContextLocator) {
            _launcherFactory = launcherFactory;
        }

        public async Task Handle(LaunchGame request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            await game.Launch(_launcherFactory, request.GetAction(game)).ConfigureAwait(false);
        }
    }
}