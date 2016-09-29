// <copyright company="SIX Networks GmbH" file="NotificationProviderHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Applications.Usecases;
using withSIX.Mini.Applications.Usecases.Main;

namespace withSIX.Mini.Applications.NotificationHandlers
{
    public class NotificationProviderHandler : DbQueryBase,
        //IAsyncNotificationHandler<ApiUserActionStarted>,
        //IAsyncNotificationHandler<ApiException>,
        //IAsyncNotificationHandler<ApiActionFinished>,
        //INotificationHandler<InstallActionCompleted>,
        IAsyncNotificationHandler<ActionNotification>, IUsecaseExecutor
    {
        static readonly TimeSpan defaultExpirationTime = TimeSpan.FromSeconds(8);
        readonly IEnumerable<INotificationProvider> _notifiers;
        private readonly IStateHandler _stateHandler;

        public NotificationProviderHandler(IDbContextLocator dbContextLocator,
            IEnumerable<INotificationProvider> notifiers, IStateHandler stateHandler) : base(dbContextLocator) {
            _notifiers = notifiers;
            _stateHandler = stateHandler;
        }

        public async Task Handle(ActionNotification notification) {
            if (!notification.DesktopNotification)
                return;
            await
                Notify(notification.Title, notification.Text, GenerateActions(notification).ToArray())
                    .ConfigureAwait(false);
        }

        private List<TrayAction> GenerateActions(ActionNotification notification) {
            var actions = new List<TrayAction>();
            if (notification.Href != null) {
                actions.Add(new TrayAction {
                    DisplayName = "Info",
                    Command = () => this.SendAsync(new OpenArbWebLink(notification.Href))
                });
            }
            if (notification.NextAction != null) {
                actions.Add(new TrayAction {
                    DisplayName = notification.NextActionInfo.Title,
                    Command = () => _stateHandler.DispatchNextAction(this.SendAsync, notification.NextAction.RequestId)
                });
            }
            return actions;
        }

        //public async Task Handle(ApiActionFinished notification) {
        //  Notify("Action", "action finished");
        //}

        //public async Task Handle(ApiException notification) {
        //  Notify("Error", notification.Exception.Message);
        //}

        /*
        public void Handle(InstallActionCompleted notification) {
            var info = GetInfo(notification);
            if (notification.Action.HideLaunchAction)
                return;
            TrayNotify(new ShowTrayNotification("content installed",
                info.Text,
                actions: CreateActions(notification, info), expirationTime: defaultExpirationTime));
        }

        static TrayAction[] CreateActions(InstallActionCompleted notification, ActionInfo info) => info.Actions.Select(
            x => new TrayAction {
                DisplayName = x.ToString(),
                Command = CreateCommand(notification, x)
            }).ToArray();

        static ActionInfo GetInfo(InstallActionCompleted notification) {
            // TODO: Consider more elegant approach to getting the info for the type of content..
            if (notification.Action.Content.Count != 1)
                return DefaultAction(notification);
            var c = notification.Action.Content.First().Content as NetworkCollection;
            // TODO: Consider double action,
            // then need to allow specify the action to execute on a collection,
            // as play currently auto joins if has server etc ;-)
            if (c != null && c.Servers.Any()) {
                return new ActionInfo {
                    Actions = new[] {PlayAction.Join, PlayAction.Launch},
                    Text = "do you wish to join the server of " + notification.Action.Name + "?"
                };
            }
            return DefaultAction(notification);
        }

        static ActionInfo DefaultAction(InstallActionCompleted notification) => new ActionInfo {
            Actions = new[] {PlayAction.Launch},
            Text = "do you want to play " + notification.Action.Name + "?"
        };

        void TrayNotify(ShowTrayNotification showTrayNotification) {
            if (!SettingsContext.Settings.Local.ShowDesktopNotifications)
                return;
            showTrayNotification.PublishToMessageBus();
        }
        */

        async Task Notify(string subject, string text, params TrayAction[] actions) {
            if (!(await SettingsContext.GetSettings()).Local.ShowDesktopNotifications)
                return;
            foreach (var n in _notifiers) {
                var t = n.Notify(subject, text, actions: actions); // hmm
            }
        }

        /*
        IReactiveCommand<Unit> CreateCommand(InstallActionCompleted notification, PlayAction playAction)
            => ReactiveCommand.CreateAsyncTask(
                async x =>
                    await this.SendAsync(
                        new LaunchContents(notification.Game.Id,
                            notification.Action.Content.Select(c => new ContentGuidSpec(c.Content.Id)).ToList(),
                            action: playAction.ToLaunchAction()))
                        .ConfigureAwait(false))
                .DefaultSetup("Play");
                */

        class ActionInfo
        {
            public string Text { get; set; }
            public IReadOnlyCollection<PlayAction> Actions { get; set; }
        }
    }
}