// <copyright company="SIX Networks GmbH" file="GetState.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games.Attributes;

namespace withSIX.Mini.Applications.Features.Main
{
    public class GetState : IQuery<ClientContentInfo>, IHaveId<Guid>
    {
        public GetState(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class GetStateHandler : ApiDbQueryBase, IAsyncRequestHandler<GetState, ClientContentInfo>
    {
        readonly IGameLockMonitor _monitor;
        readonly IStateHandler _stateHandler;

        public GetStateHandler(IDbContextLocator dbContextLocator, IGameLockMonitor monitor, IStateHandler stateHandler)
            : base(dbContextLocator) {
            _monitor = monitor;
            _stateHandler = stateHandler;
        }

        public Task<ClientContentInfo> Handle(GetState request) => BuildClientContentInfo(request);

        private async Task<ClientContentInfo> BuildClientContentInfo(GetState request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            var gameLock = await _monitor.GetObservable(request.Id).ConfigureAwait(false);
            var gameStateHandler = _stateHandler.Games[game.Id];
            return new ClientContentInfo {
                GameLock = gameLock.IsLocked,
                CanAbort = gameLock.CanAbort,
                Content = gameStateHandler.State.ToDictionary(x => x.Key, x => x.Value),
                Dlcs = game.InstalledDlcs().ToList(),
                IsRunning = gameStateHandler.IsRunning,
                ActionInfo = _stateHandler.Current,
                UserErrors = _stateHandler.UserErrors.ToList(),
                Mappings = game.Mappings.ToDictionary(x => x.Key, x => x.Value)
            };
        }
    }

    public class ClientContentInfo
    {
        public Dictionary<Guid, ContentStatus> Content { get; set; }
        public bool GameLock { get; set; }
        public bool IsRunning { get; set; }
        public bool CanAbort { get; set; }
        public ActionTabState ActionInfo { get; set; }
        public List<UserErrorModel2> UserErrors { get; set; }
        public List<Dlc> Dlcs { get; set; }
        public Dictionary<string, Guid> Mappings { get; set; }
    }

    [Obsolete("Convert from original UserErrorModel?")]
    public class UserErrorModel2 : IHaveId<Guid>
    {
        public UserErrorModel2(dynamic error, List<RecoveryOptionModel> recoveryOptions) {
            ErrorMessage = error.ErrorMessage;
            ErrorCauseOrResolution = error.ErrorCauseOrResolution;
            RecoveryOptions = recoveryOptions;
            Type = error.ContextInfo?["$$$Type"] ?? error.GetType().ToString();
            UserError = error;
            Data = error.ContextInfo;
        }

        [JsonIgnore]
        public object UserError { get; }

        public Dictionary<string, object> Data { get; set; }
        public string ErrorMessage { get; }
        public string ErrorCauseOrResolution { get; }
        public List<RecoveryOptionModel> RecoveryOptions { get; }
        public string Type { get; }

        public Guid Id { get; } = Guid.NewGuid();
    }

    public class RecoveryOptionModel
    {
        public string CommandName { get; set; }
        //public string RecoveryResult { get; set; }
    }

    public class NextActionInfo
    {
        public NextActionInfo(string title, string text = null) {
            if (title == null) throw new ArgumentNullException(nameof(title));
            Title = title;
            Text = text;
        }

        public string Title { get; }
        public string Text { get; }
        public Guid RequestId { get; set; }
        public Uri Href { get; set; }
    }
}