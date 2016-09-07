// <copyright company="SIX Networks GmbH" file="GetState.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReactiveUI;
using MediatR;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using withSIX.Api.Models.Content.v3;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class GetState : IAsyncQuery<ClientContentInfo>, IHaveId<Guid>
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
            var gameLock = await _monitor.GetObservable(request.Id).FirstAsync();
            var gameStateHandler = _stateHandler.Games[game.Id];
            return new ClientContentInfo {
                GameLock = gameLock.IsLocked,
                CanAbort = gameLock.CanAbort,
                Content = gameStateHandler.State.ToDictionary(x => x.Key, x => x.Value),
                IsRunning = gameStateHandler.IsRunning,
                ActionInfo = _stateHandler.Current,
                UserErrors = _stateHandler.UserErrors.ToList()
            };
        }
    }

    public class ClientContentInfo
    {
        public Dictionary<Guid, ContentStatus> Content { get; set; }
        public bool GameLock { get; set; }
        public bool IsRunning { get; set; }
        public bool GlobalLock { get; set; }
        public bool CanAbort { get; set; }
        public ActionTabState ActionInfo { get; set; }
        public List<UserErrorModel> UserErrors { get; set; }
    }

    public class UserErrorModel : IHaveId<Guid>
    {
        public UserErrorModel(UserError error) {
            ErrorMessage = error.ErrorMessage;
            ErrorCauseOrResolution = error.ErrorCauseOrResolution;
            RecoveryOptions =
                error.RecoveryOptions.Select(
                    x =>
                        new RecoveryOptionModel {
                            CommandName = x.CommandName
                        }).ToList();
            Type = error.GetType().ToString();
            UserError = error;
            Data = error.ContextInfo;
        }

        [JsonIgnore]
        public UserError UserError { get; }

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
            Contract.Requires<ArgumentNullException>(title != null);
            Title = title;
            Text = text;
        }

        public string Title { get; }
        public string Text { get; }
        public Guid RequestId { get; set; }
        public Uri Href { get; set; }
    }
}