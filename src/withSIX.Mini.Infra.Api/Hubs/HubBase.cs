// <copyright company="SIX Networks GmbH" file="HubBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using withSIX.Api.Models.Exceptions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Presentation;
using withSIX.Mini.Applications;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Presentation.Owin.Core;

namespace withSIX.Mini.Infra.Api.Hubs
{
    public abstract class HubBase<T> : Hub<T> where T : class
    {
        protected Task<TResponseData> SendAsync<TResponseData>(ICompositeCommand<TResponseData> command) {
            var requestId = Guid.NewGuid();
            return A.ApiAction(ct => A.Excecutor.SendAsync(command), command,
                CreateException, requestId, Context.ConnectionId, Context.User, RequestScopeService.Instance);
        }

        // TODO: We need to actually Create a dictionary from the error data instead, so we can drop the crappy Serializing stuf
        private Exception CreateException(string msg, Exception inner)
            => new HubException(msg, (inner as UserException)?.GetObjectData());

        protected Task<TResponse> SendAsync<TResponse>(IAsyncRequest<TResponse> command)
            => A.ApiAction(ct => A.Excecutor.SendAsync(command), command,
                CreateException, Guid.NewGuid(), Context.ConnectionId, Context.User, RequestScopeService.Instance);

        protected Task<TResponse> SendAsync<TResponse>(ICancellableQuery<TResponse> command, Guid requestId)
            => A.ApiAction(async ct => await A.Excecutor.SendAsync(command, ct).ConfigureAwait(false), command,
                CreateException, requestId, Context.ConnectionId, Context.User, RequestScopeService.Instance);

        protected Task<Unit> DispatchNextAction(Guid requestId)
            => Cheat.Mediator.DispatchNextAction(SendAsync, requestId);

        public async Task Cancel(Guid requestId) => A.CancellationTokenMapping.Cancel(requestId);
    }
}