// <copyright company="SIX Networks GmbH" file="StateHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ReactiveUI;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Core.Infra.Services;
using withSIX.Mini.Applications;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services;
using withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace withSIX.Mini.Infra.Api
{
    public class StateHandler : IInfrastructureService, IStateHandler
    {
        readonly IDbContextLocator _locator;
        readonly Subject<StatusModel> _subject;
        readonly Subject<ActionTabState> _subject2;
        private ActionTabState _current;
        private AppState _state = new AppState(AppUpdateState.Uptodate, null);
        StatusModel _status = StatusModel.Default;

        public StateHandler(IDbContextLocator locator, IMessageBusProxy messageBus) {
            _locator = locator;
            messageBus.Listen<StatusChanged>()
                .Subscribe(Handle);
            messageBus.Listen<ContentStatusChanged>()
                .Subscribe(Handle);
            messageBus.Listen<ContentInstalled>()
                .Subscribe(Handle);
            messageBus.Listen<UninstallActionCompleted>()
                .Subscribe(Handle);
            messageBus.Listen<GameLaunched>()
                .Subscribe(Handle);
            messageBus.Listen<GameTerminated>()
                .Subscribe(Handle);
            messageBus.Listen<RecentItemRemoved>()
                .Subscribe(Handle);
            messageBus.Listen<ContentUsed>()
                .Subscribe(Handle);
            messageBus.Listen<ActionNotification>()
                .Subscribe(Handle);

            messageBus.Listen<ExtensionStateChanged>()
                .ConcatTask(Handle)
                .Subscribe();

            _subject2 = new Subject<ActionTabState>();
            _subject = new Subject<StatusModel>();


            _subject.Subscribe(Handle);
            _subject2.ConcatTask(x => GetActionStateUpdate(x).Raise())
                .Subscribe();
        }

        public StatusModel Status
        {
            get { return _status; }
            private set
            {
                if (EqualityComparer<StatusModel>.Default.Equals(_status, value))
                    return;
                _status = value;
                _subject.OnNext(value);
            }
        }

        public ClientInfo ClientInfo { get; private set; }
        public Guid SelectedGameId { get; set; }

        public UserErrorModel2[] UserErrors { get; private set; } = new UserErrorModel2[0];

        public ActionTabState Current
        {
            get { return _current; }
            private set
            {
                if (EqualityComparer<ActionTabState>.Default.Equals(_current, value))
                    return;
                _current = value;
                _subject2.OnNext(value);
            }
        }

        public Task DispatchNextAction(Func<ICommand, CancellationToken, Task> dispatcher, Guid actionId, CancellationToken ct) {
            var action = Current?.NextAction;
            if (action == null)
                throw new ValidationException("There was no next action available");
            var actionInfo = Current?.NextActionInfo;
            if (actionInfo.RequestId != actionId)
                throw new ValidationException("This action is no longer a valid action");
            Current.NextAction = null;
            Current.NextActionInfo = null;
            return dispatcher(action, ct);
        }

        public async Task AddUserError(UserErrorModel2 error) {
            UserErrors = UserErrors.Concat(new[] {error}).ToArray();
            await new UserErrorAdded(error).Raise().ConfigureAwait(false);
        }

        public Task StartUpdating() => Raise(new AppState(AppUpdateState.Updating, _state.Version));

        public Task UpdateAvailable(string version, AppUpdateState appUpdateState = AppUpdateState.UpdateAvailable)
            => Raise(new AppState(appUpdateState, version));


        public async Task ResolveError(Guid id, string result, Dictionary<string, object> data) {
            var error = UserErrors.FindOrThrow(id);
            var ue = (UserError) error.UserError;
            if ((data != null) && (ue.ContextInfo != null))
                data.ForEach(x => ue.ContextInfo[x.Key] = x.Value);
            ue.RecoveryOptions.First(x => x.CommandName == result).Execute(null);
            UserErrors = UserErrors.Except(new[] {error}).ToArray();
            await new UserErrorResolved(id, result).Raise().ConfigureAwait(false);
        }

        public IObservable<ActionTabState> StatusObservable => _subject2.StartWith(Current).Where(x => x != null);
        public IDictionary<Guid, GameStateHandler> Games { get; } = new Dictionary<Guid, GameStateHandler>();

        public async Task Initialize() {
            await HandleInitialClientInfo().ConfigureAwait(false);
            await HandleInitialGameStates().ConfigureAwait(false);
        }

        Task Handle(ExtensionStateChanged evt) => UpdateClientInfo(evt.State);

        private Task Raise(AppState appState) {
            _state = appState;
            return UpdateClientInfo();
        }

        Task UpdateClientInfo()
            => new ClientInfoUpdated(ClientInfo = new ClientInfo(_state, ClientInfo.ExtensionInstalled)).Raise();

        Task UpdateClientInfo(bool extensionInstalled)
            => new ClientInfoUpdated(ClientInfo = new ClientInfo(_state, extensionInstalled)).Raise();

        private async Task HandleInitialClientInfo() {
            var settings = await _locator.GetReadOnlySettingsContext().GetSettings().ConfigureAwait(false);
            var versionInfoChanged = !Consts.InternalVersion.Equals(settings.Local.CurrentVersion);
            var newVersionInstalled = (settings.Local.CurrentVersion != null) &&
                                      versionInfoChanged;
            if (versionInfoChanged) {
                settings.Local.CurrentVersion = Consts.InternalVersion;
                _state = new AppState(AppUpdateState.UpdateInstalled, _state.Version);
            }
            ClientInfo = new ClientInfo(_state, settings.Local.InstalledExtension);
        }

        private async Task HandleInitialGameStates() {
            // TODO: Only states of games that are actually used in the session (so progressively?)
            var context = _locator.GetReadOnlyGameContext();
            await context.LoadAll().ConfigureAwait(false);
            foreach (var g in context.Games) {
                Games[g.Id] =
                    new GameStateHandler(
                        new ConcurrentDictionary<Guid, ContentStatus>(
                            g.InstalledContent.Concat(g.IncompleteContent).GetStates()
                                .ToDictionary(x => x.Key, x => x.Value)));
            }
        }

        void Handle(GameLockChanged message) {
            if (message.CanAbort)
                return;
            if (Current != null) {
                Current.NextAction = null;
                Current.NextActionInfo = null;
            }
        }

        void Handle(RecentItemRemoved message) {
            var gameState = Games[message.Content.GameId].State;
            ContentStatus state;
            if (gameState.TryGetValue(message.Content.Id, out state))
                state.LastUsed = null;
        }

        void Handle(ContentUsed message) {
            var gameState = Games[message.Content.GameId].State;
            ContentStatus state;
            if (gameState.TryGetValue(message.Content.Id, out state))
                state.LastUsed = message.Content.RecentInfo.LastUsed;
        }

        void Handle(GameLaunched message) {
            Games[message.Game.Id].IsRunning = true;
            var t = Task.Run(async () => {
                try {
                    using (var process = Process.GetProcessById(message.ProcessId))
                        process.WaitForExit();
                } finally {
                    await new GameTerminated(message.Game, message.ProcessId).Raise().ConfigureAwait(false);
                }
            });
        }

        void Handle(GameTerminated message) {}

        void Handle(ActionNotification message) {
            var actionTabState = message.MapTo<ActionTabState>();
            if (actionTabState.Type == ActionType.Start)
                actionTabState.Progress = 0;
            actionTabState.ChildAction = new ChildActionState {
                Title = message.Title,
                Text = message.Text,
                Href = message.Href
            };
            Current = actionTabState;
        }

        void Handle(StatusModel sm) {
            var childAction = Current.ChildAction;
            HandleChildAction(sm, childAction);
            Current.Progress = sm.Info.Progress;
            //Current.Speed = sm.Info.Speed;
            _subject2.OnNext(Current);
        }

        private static void HandleChildAction(StatusModel sm, ChildActionState childAction) {
            childAction.Details = sm.Info.Text;
            var ac = GetActiveComponent(sm.Info);
            if (ac != null) {
                childAction.Progress = ac.Progress;
                childAction.Speed = ac.Speed;
                childAction.Title = ac.Title;
            } else
                childAction.Title = "Processing";
        }

        private ActionTabStateUpdate GetActionStateUpdate(ActionTabState sm) => new ActionTabStateUpdate {
            Progress = sm.Progress,
            ChildAction = sm.ChildAction
        };

        private static FlatProgressInfo GetActiveComponent(ProgressInfo info) {
            if (info.Components.Count >= 3)
                return info.Components[2];
            if (info.Components.Count == 2)
                return info.Components[1];
            return null;
        }

        void Handle(UninstallActionCompleted message) {
            var gameState = Games[message.Game.Id].State;
            foreach (var c in message.UninstallLocalContentAction.Content) {
                ContentStatus cs;
                gameState.TryRemove(c.Content.Id, out cs);
            }
        }

        void Handle(ContentInstalled message) {
            var gameState = Games[message.GameId].State;
            foreach (var c in message.Content)
                gameState[c.Id] = c.MapTo<ContentStatus>();
        }

        void Handle(ContentStatusChanged message) {
            var gameState = Games[message.Content.GameId].State;
            ContentStatus state;
            switch (message.State) {
            case ItemState.NotInstalled: {
                gameState.TryRemove(message.Content.Id, out state);
                return;
            }
            }
            if (gameState.TryGetValue(message.Content.Id, out state))
                message.MapTo(state);
            else
                gameState[message.Content.Id] = state = message.MapTo<ContentStatus>();
        }

        void Handle(StatusChanged message) {
            switch (message.Status) {
            case Core.Games.Services.ContentInstaller.Status.Synchronized: {
                Status = StatusModel.Default;
                break;
            }
            case Core.Games.Services.ContentInstaller.Status.Synchronizing: {
                Status = GetBusyState(message, SixIconFont.withSIX_icon_Reload);
                break;
            }
            case Core.Games.Services.ContentInstaller.Status.Preparing: {
                Status = GetBusyState(message, SixIconFont.withSIX_icon_Cloud);
                break;
            }
            default: {
                throw new NotSupportedException(message.Status + " is not supported");
            }
            }
        }

        private static StatusModel GetBusyState(StatusChanged message, string icon)
            => new StatusModel(message.Status.ToString(), icon, message.Info, true,
                SixColors.SixOrange);
    }
}