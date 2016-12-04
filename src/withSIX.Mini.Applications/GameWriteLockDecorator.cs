// <copyright company="SIX Networks GmbH" file="GameWriteLockDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Features;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services;

namespace withSIX.Mini.Applications
{
    public class GameWriteLockDecorator : MediatorDecoratorBase
    {
        private readonly IGameLocker _gameLocker;

        public GameWriteLockDecorator(IMediator target, IGameLocker gameLocker) : base(target) {
            _gameLocker = gameLocker;
        }

        public override TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            if (!ShouldLock(request))
                return Decorated.Send(request);
            var gameId = (request as IHaveGameId).GameId;
            return Handle(request, gameId).WaitAndUnwrapException();
        }

        public override async Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            if (!ShouldLock(request))
                return await base.SendAsync(request).ConfigureAwait(false);
            var gameId = ((IHaveGameId) request).GameId;
            using (var i = await _gameLocker.ConfirmLock(gameId, true).ConfigureAwait(false)) {
                HandleCTS(request, i.Token);
                return await base.SendAsync(request).ConfigureAwait(false);
            }
        }

        [Obsolete("Canceltoken not used")]
        public override async Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) {
            if (!ShouldLock(request))
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var gameId = ((IHaveGameId) request).GameId;
            using (var i = await _gameLocker.ConfirmLock(gameId, true).ConfigureAwait(false)) {
                HandleCTS(request, i.Token);
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<TResponseData> Handle<TResponseData>(IRequest<TResponseData> request, Guid gameId) {
            using (var i = await _gameLocker.ConfirmLock(gameId, true).ConfigureAwait(false)) {
                HandleCTS(request, i.Token);
                return Decorated.Send(request);
            }
        }

        private void HandleCTS(object request, CancellationToken token) {
            var a = request as ICancellable;
            if (a != null)
                a.CancelToken = token;
        }

        private static bool ShouldLock(object request)
            => request is IWrite && request is IHaveGameId && !(request is IExcludeGameWriteLock);
    }
}