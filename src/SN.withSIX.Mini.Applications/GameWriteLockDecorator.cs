// <copyright company="SIX Networks GmbH" file="GameWriteLockDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Applications
{
    class CTSHandler : IDisposable
    {
        private readonly INeedCancellationTokenSource _n;

        public CTSHandler(INeedCancellationTokenSource n) {
            Contract.Requires<ArgumentNullException>(n != null);
            _n = n;
            n.CTS = new DoneCancellationTokenSource();
        }

        public void Dispose() {
            _n.CTS?.Dispose();
            _n.CTS = null;
        }

        public static CTSHandler Build(INeedCancellationTokenSource n) => n == null ? null : new CTSHandler(n);
    }

    public class GameWriteLockDecorator : IMediator
    {
        private readonly IGameLocker _gameLocker;
        private readonly IMediator _target;

        public GameWriteLockDecorator(IMediator target, IGameLocker gameLocker) {
            _target = target;
            _gameLocker = gameLocker;
        }

        public TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            using (CTSHandler.Build(request as INeedCancellationTokenSource)) {
                if (!ShouldLock(request))
                    return _target.Send(request);
                var gameId = (request as IHaveGameId).GameId;
                return Handle(request, gameId).WaitAndUnwrapException();
            }
        }

        public async Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            using (CTSHandler.Build(request as INeedCancellationTokenSource)) {
                if (!ShouldLock(request))
                    return await _target.SendAsync(request).ConfigureAwait(false);
                var gameId = ((IHaveGameId) request).GameId;
                using (await _gameLocker.ConfirmLock(gameId, true).ConfigureAwait(false))
                    return await _target.SendAsync(request).ConfigureAwait(false);
            }
        }

        public void Publish(INotification notification) => _target.Publish(notification);

        public Task PublishAsync(IAsyncNotification notification) => _target.PublishAsync(notification);

        public Task PublishAsync(ICancellableAsyncNotification notification, CancellationToken cancellationToken) => _target.PublishAsync(notification, cancellationToken);

        public async Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request, CancellationToken cancellationToken) {
            using (CTSHandler.Build(request as INeedCancellationTokenSource)) {
                if (!ShouldLock(request))
                    return await _target.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var gameId = ((IHaveGameId)request).GameId;
                using (await _gameLocker.ConfirmLock(gameId, true).ConfigureAwait(false))
                    return await _target.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<TResponseData> Handle<TResponseData>(IRequest<TResponseData> request, Guid gameId) {
            using (await _gameLocker.ConfirmLock(gameId, true).ConfigureAwait(false))
                return _target.Send(request);
        }

        private static bool ShouldLock(object request)
            => request is IWrite && request is IHaveGameId && !(request is IExcludeGameWriteLock);
    }
}