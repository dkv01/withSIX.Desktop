// <copyright company="SIX Networks GmbH" file="StateHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Applications;
using withSIX.Core.Applications.Services;
using withSIX.Core.Helpers;
using withSIX.Mini.Applications.Features;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Sync.Core.Legacy.Status;
using Status = withSIX.Mini.Core.Games.Services.ContentInstaller.Status;

namespace withSIX.Mini.Applications.Services
{
    public class StatusModelChanged : ISyncDomainEvent
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
        UserErrorModel2[] UserErrors { get; }
        ClientInfo ClientInfo { get; }
        Guid SelectedGameId { get; set; }
        Task Initialize();

        Task DispatchNextAction(Func<ICommand, CancellationToken, Task> dispatcher, Guid requestId,
            CancellationToken ct);

        Task ResolveError(Guid id, string result, Dictionary<string, object> data);
        Task AddUserError(UserErrorModel2 error);
        Task StartUpdating();
        Task UpdateAvailable(string version, AppUpdateState appUpdateState = AppUpdateState.UpdateAvailable);
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
        public ICommandBase NextAction { get; set; }
        public ChildActionState ChildAction { get; set; }
    }

    public class ActionTabStateUpdate : ISyncDomainEvent
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
            if (info == null) throw new ArgumentNullException(nameof(info));
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
            if (This == null) throw new ArgumentNullException(nameof(This));
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

    public class UserErrorResolved : ISyncDomainEvent
    {
        public UserErrorResolved(Guid id, string result) {
            Id = id;
            Result = result;
        }

        public Guid Id { get; }
        public string Result { get; }
    }

    public class UserErrorAdded : ISyncDomainEvent
    {
        public UserErrorAdded(UserErrorModel2 userError) {
            UserError = userError;
        }

        public UserErrorModel2 UserError { get; }
    }

    public class ClientInfoUpdated : ISyncDomainEvent
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