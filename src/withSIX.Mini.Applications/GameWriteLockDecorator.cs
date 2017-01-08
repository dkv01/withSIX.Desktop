// <copyright company="SIX Networks GmbH" file="GameWriteLockDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using MediatR;
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

        public override async Task<TResponseData> Send<TResponseData>(IRequest<TResponseData> request, CancellationToken cancelToken = default(CancellationToken)) {
            if (!ShouldLock(request))
                return await base.Send(request, cancelToken).ConfigureAwait(false);
            var gameId = ((IHaveGameId) request).GameId;
            using (var i = await _gameLocker.ConfirmLock(gameId, true).ConfigureAwait(false)) {
                HandleCTS(request, i.Token);
                return await base.Send(request, cancelToken).ConfigureAwait(false);
            }
        }

        public override async Task Send(IRequest request, CancellationToken cancelToken = default(CancellationToken))
        {
            if (!ShouldLock(request))
                await base.Send(request, cancelToken).ConfigureAwait(false);
            var gameId = ((IHaveGameId)request).GameId;
            using (var i = await _gameLocker.ConfirmLock(gameId, true).ConfigureAwait(false))
            {
                HandleCTS(request, i.Token);
                await base.Send(request, cancelToken).ConfigureAwait(false);
            }
        }

        /*
        private async Task<TResponseData> Handle<TResponseData>(IRequest<TResponseData> request, Guid gameId) {
            using (var i = await _gameLocker.ConfirmLock(gameId, true).ConfigureAwait(false)) {
                HandleCTS(request, i.Token);
                return Decorated.Send(request);
            }
        }*/

        private void HandleCTS(object request, CancellationToken token) {
            var a = request as ICancellable;
            if (a != null)
                a.CancelToken = token;
        }

        private static bool ShouldLock(object request)
            => request is IWrite && request is IHaveGameId && !(request is IExcludeGameWriteLock);
    }
}