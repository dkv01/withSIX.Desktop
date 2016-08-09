// <copyright company="SIX Networks GmbH" file="ActionNotifierDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Core.Games;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Applications
{
    public class ActionNotifierDecorator : IMediator
    {
        private readonly IGameSwitcher _gameSwitcher;
        private readonly IDbContextLocator _locator;
        private readonly INetworkContentSyncer _networkSyncer;
        readonly IMediator _target;

        public ActionNotifierDecorator(IMediator target, IDbContextLocator locator, IGameSwitcher gameSwitcher,
            INetworkContentSyncer networkSyncer) {
            _target = target;
            _locator = locator;
            _gameSwitcher = gameSwitcher;
            _networkSyncer = networkSyncer;
        }

        public TResponseData Send<TResponseData>(IRequest<TResponseData> request)
            =>
                Perform(request,
                    () => Task.Factory.StartNew(() => _target.Send(request), TaskCreationOptions.LongRunning))
                    .WaitAndUnwrapException();

        public Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request)
            => Perform(request, () => _target.SendAsync(request));

        public Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken)
            => Perform(request, () => _target.SendAsync(request, cancellationToken));

        public void Publish(INotification notification) => _target.Publish(notification);

        public Task PublishAsync(IAsyncNotification notification) => _target.PublishAsync(notification);
        public Task PublishAsync(ICancellableAsyncNotification notification, CancellationToken cancellationToken) => _target.PublishAsync(notification, cancellationToken);

        private Task<TResponseData> Perform<TResponseData>(object request, Func<Task<TResponseData>> exec) {
            var act = request as INotifyAction;
            return act == null ? HandleNormal(request, exec) : HandleNotifyAction(request, exec, act);
        }

        private async Task<TResponseData> HandleNotifyAction<TResponseData>(object request,
            Func<Task<TResponseData>> exec, INotifyAction act) {
            var action = await PrepareAction(request, act).ConfigureAwait(false);
            return await act.NotifyAction(exec, action.Name, action.Href).ConfigureAwait(false);
        }

        // Preparation stage..
        // another option would be that each request, even single content, should have a name included, so that we don't need the Game to generate the name for the request..
        // TODO: Or should we show 'Preparing' here, and no abort or ?
        private Task<IContentAction<IContent>> PrepareAction(object request, INotifyAction act) =>
            act.PerformAction(async () => {
                var shouldSave = await HandleGameContents(request, act.GameId).ConfigureAwait(false);
                var gc = _locator.GetGameContext();
                var game = await gc.FindGameOrThrowAsync(act).ConfigureAwait(false);
                if (!game.InstalledState.IsInstalled) {
                    throw new GameNotInstalledException("The requested game appears not installed: " +
                                                        game.Metadata.Name);
                }
                var syncer = request as INeedSynchronization;
                if (syncer != null) {
                    await syncer.Synchronize(game, _networkSyncer).ConfigureAwait(false);
                    shouldSave = true;
                }

                var action = act.GetAction(game);
                // We do this here because it's a pre-action that must be saved before starting the main action.
                // Abstracting it in a separate command, either ran manually or auto, could work
                // however we would not be able to optimize the save dedup between this and handlegamecontents.
                if (request is IUseContent) {
                    game.UseContent(action);
                    shouldSave = true;
                }
                // intermediary save..
                if (shouldSave)
                    await gc.SaveChanges().ConfigureAwait(false);
                return action;
            }, (request as IHaveRequestName)?.Name ?? "One moment...");

        private async Task<TResponseData> HandleNormal<TResponseData>(object request, Func<Task<TResponseData>> exec) {
            var needGameContents = request as INeedGameContents;
            if (needGameContents != null) {
                if (await HandleGameContents(request, needGameContents.GameId).ConfigureAwait(false)) {
                    var gc = _locator.GetGameContext();
                    // We save intermediately because we don't just want to loose this on action failure..
                    await gc.SaveChanges().ConfigureAwait(false);
                }
            }
            return await exec().ConfigureAwait(false);
        }

        private async Task<bool> HandleGameContents(object request, Guid gameId) {
            if (request is INeedGameContents) {
                // TODO: Handle exception with an Ignore option, e.g in case internet/platform is down!
                await _gameSwitcher.UpdateGameState(gameId).ConfigureAwait(false);
                return true;
            }
            return false;
        }
    }
}