// <copyright company="SIX Networks GmbH" file="StateHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MoreLinq;
using Newtonsoft.Json;
using ReactiveUI;
using ShortBus;
using SN.withSIX.Api.Models.Exceptions;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace SN.withSIX.Mini.Applications.Services
{
    public class StatusModelChanged : IDomainEvent
    {
        public StatusModelChanged(StatusModel model) {
            Status = model;
        }

        public StatusModel Status { get; }
    }

    // TODO: Sort out ViewModel vs Service. Currently we use this object to centralize the information for both the
    // XAML ViewModels, as well as the JS ViewModels
    public interface IStateHandler
    {
        IObservable<ActionTabState> StatusObservable { get; }
        IDictionary<Guid, GameStateHandler> Games { get; }
        ActionTabState Current { get; }
        UserErrorModel[] UserErrors { get; }
        ClientInfo ClientInfo { get; }
        Guid SelectedGameId { get; set; }
        Task Initialize();
        Task<UnitType> DispatchNextAction(Func<IAsyncVoidCommand, Task<UnitType>> dispatcher, Guid requestId);
        Task ResolveError(Guid id, string result, Dictionary<string, object> data);
        Task AddUserError(UserErrorModel error);
        Task StartUpdating();
        Task UpdateAvailable(string version, AppUpdateState appUpdateState = AppUpdateState.UpdateAvailable);
    }

    public class StateHandler : IApplicationService, IStateHandler
    {
        readonly IDbContextLocator _locator;
        readonly Subject<StatusModel> _subject;
        readonly Subject<ActionTabState> _subject2;
        private ActionTabState _current;
        private AppState _state = new AppState(AppUpdateState.Uptodate, null);
        StatusModel _status = StatusModel.Default;

        public StateHandler(IDbContextLocator locator, IMessageBus messageBus) {
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

            //this.Listen<GameLockChanged>()
            //  .Subscribe(Handle);

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

        public UserErrorModel[] UserErrors { get; private set; } = new UserErrorModel[0];

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

        public Task<UnitType> DispatchNextAction(Func<IAsyncVoidCommand, Task<UnitType>> dispatcher, Guid actionId) {
            var action = Current?.NextAction;
            if (action == null)
                throw new ValidationException("There was no next action available");
            var actionInfo = Current?.NextActionInfo;
            if (actionInfo.RequestId != actionId)
                throw new ValidationException("This action is no longer a valid action");
            Current.NextAction = null;
            Current.NextActionInfo = null;
            return dispatcher(action);
        }

        public async Task AddUserError(UserErrorModel error) {
            UserErrors = UserErrors.Concat(new[] {error}).ToArray();
            await new UserErrorAdded(error).Raise().ConfigureAwait(false);
        }

        public Task StartUpdating() => Raise(new AppState(AppUpdateState.Updating, _state.Version));

        public Task UpdateAvailable(string version, AppUpdateState appUpdateState = AppUpdateState.UpdateAvailable)
            => Raise(new AppState(appUpdateState, version));


        public async Task ResolveError(Guid id, string result, Dictionary<string, object> data) {
            var error = UserErrors.FindOrThrow(id);
            if (data != null && error.UserError.ContextInfo != null)
                data.ForEach(x => error.UserError.ContextInfo[x.Key] = x.Value);
            error.UserError.RecoveryOptions.First(x => x.CommandName == result).Execute(null);
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
            var newVersionInstalled = settings.Local.CurrentVersion != null &&
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
            case Mini.Core.Games.Services.ContentInstaller.Status.Synchronized: {
                Status = StatusModel.Default;
                break;
            }
            case Mini.Core.Games.Services.ContentInstaller.Status.Synchronizing: {
                Status = GetBusyState(message, SixIconFont.withSIX_icon_Reload);
                break;
            }
            case Mini.Core.Games.Services.ContentInstaller.Status.Preparing: {
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

    public class ActionTabState
    {
        public Guid GameId { get; set; }
        public double? Progress { get; set; }
        //public long? Speed { get; set; }
        public string Text { get; set; }
        public ActionType Type { get; set; }
        public NextActionInfo NextActionInfo { get; set; }
        [JsonIgnore]
        public IAsyncVoidCommandBase NextAction { get; set; }
        public ChildActionState ChildAction { get; set; }
    }

    public class ActionTabStateUpdate : IDomainEvent
    {
        public Guid GameId { get; set; }
        public double? Progress { get; set; }
        public ChildActionState ChildAction { get; set; }
    }

    public class ChildActionState
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public Uri Href { get; set; }
        public string Details { get; set; }
        public double? Progress { get; set; }
        public long? Speed { get; set; }
    }

    class GameTerminated : IDomainEvent
    {
        public GameTerminated(Game game, int processId) {
            Game = game;
            ProcessId = processId;
        }

        public Game Game { get; }
        public int ProcessId { get; }
    }

    public class GameStateHandler
    {
        public GameStateHandler(ConcurrentDictionary<Guid, ContentStatus> states) {
            State = states;
        }

        public ConcurrentDictionary<Guid, ContentStatus> State { get; }
        public bool IsRunning { get; set; }
    }

    public class StatusModel : IEquatable<StatusModel>
    {
        public static readonly StatusModel Default = new StatusModel(Status.Synchronized.ToString(),
            SixIconFont.withSIX_icon_Hexagon, ProgressInfo.Default,
            color: SixColors.SixGreen);

        public StatusModel(string text, string icon, ProgressInfo info, bool acting = false,
            string color = null) {
            Contract.Requires<ArgumentNullException>(info != null);
            Text = text;
            Icon = icon;
            Acting = acting;
            Color = color;
            Info = info;
        }

        public ProgressInfo Info { get; }
        public string Text { get; }
        public string Icon { get; }
        public string Color { get; }
        public bool Acting { get; }
        // TODO: Merge Acting and State?
        public State State { get; }

        public bool Equals(StatusModel other) => other != null
                                                 && (ReferenceEquals(this, other)
                                                     || other.GetHashCode() == GetHashCode());

        public override int GetHashCode() => HashCode.Start
            .Hash(Text)
            .Hash(Icon)
            .Hash(Color)
            .Hash(Info)
            .Hash(Acting)
            .Hash(State);

        private string ToText() => Text + ": " + ToProgressText();

        public string GetText() => Acting ? ToText() : Text;

        public string ToProgressText() => Info.Text ?? $"{Info.Progress:0.##}%";

        public override bool Equals(object obj) => Equals(obj as StatusModel);
    }

    public enum State
    {
        Normal,
        Paused,
        Error
    }

    public static class FlatProgressInfoExtensions
    {
        public static List<FlatProgressInfo> Flatten(this ProgressComponent This) {
            Contract.Requires<ArgumentNullException>(This != null);
            var list = new List<FlatProgressInfo> {This.MapTo<FlatProgressInfo>()};
            var active = This.GetFirstActive();
            if (active == null)
                return list;
            var c = active as ProgressComponent;
            if (c != null)
                list.AddRange(c.Flatten());
            else
                list.Add(active.MapTo<FlatProgressInfo>());
            return list;
        }
    }

    public class UserErrorResolved : IDomainEvent
    {
        public UserErrorResolved(Guid id, string result) {
            Id = id;
            Result = result;
        }

        public Guid Id { get; }
        public string Result { get; }
    }

    public class UserErrorAdded : IDomainEvent
    {
        public UserErrorAdded(UserErrorModel userError) {
            UserError = userError;
        }

        public UserErrorModel UserError { get; }
    }

    public class ClientInfoUpdated : IDomainEvent
    {
        public ClientInfoUpdated(ClientInfo info) {
            Info = info;
        }

        public ClientInfo Info { get; }
    }

    public class AppState
    {
        public AppState(AppUpdateState updateState, string version) {
            UpdateState = updateState;
            Version = version;
        }

        public AppUpdateState UpdateState { get; }
        public string Version { get; }
    }

    public enum AppUpdateState
    {
        Uptodate, // 0
        UpdateInstalled, // 1
        UpdateAvailable, // 2
        UpdateDownloading, // 3
        Updating // 4
    }
}