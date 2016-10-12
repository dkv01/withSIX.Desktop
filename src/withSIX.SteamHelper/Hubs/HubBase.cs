// <copyright company="SIX Networks GmbH" file="HubBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using withSIX.Api.Models.Exceptions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Infra.Services;
using withSIX.Mini.Applications;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Services;

namespace withSIX.Steam.Presentation.Hubs
{
    static class A
    {
        // Static because new commands create new Hub instances
        internal static readonly CancellationTokenMapping CancellationTokenMapping = new CancellationTokenMapping();
        internal static readonly Excecutor Excecutor = new Excecutor();
    }

    public abstract class HubBase<T> : Hub<T>, IUsecaseExecutor where T : class
    {
        protected Task<TResponseData> SendAsync<TResponseData>(ICompositeCommand<TResponseData> command) =>
            A.Excecutor.ApiAction(() => {
                    HandleValues(command, Guid.NewGuid());
                    return UsecaseExecutorExtensions.SendAsync(this, command);
                }, command,
                CreateException);

        // TODO: We need to actually Create a dictionary from the error data instead, so we can drop the crappy Serializing stuf
        private Exception CreateException(string msg, Exception inner)
            => new HubException(msg, (inner as UserException)?.GetObjectData());

        protected Task<TResponse> SendAsync<TResponse>(IAsyncRequest<TResponse> command) => A.Excecutor.ApiAction(
            () => {
                HandleValues(command, Guid.NewGuid());
                return UsecaseExecutorExtensions.SendAsync(this, command);
            }, command,
            CreateException);

        protected Task<TResponse> SendAsync<TResponse>(ICancellableQuery<TResponse> command, Guid requestId)
            => A.Excecutor.ApiAction(async () => {
                    var cancellationToken = A.CancellationTokenMapping.AddToken(requestId);
                    try {
                        HandleValues(command, requestId);
                        return await this.SendAsync(command, cancellationToken).ConfigureAwait(false);
                    } finally {
                        A.CancellationTokenMapping.Remove(requestId);
                    }
                }, command,
                CreateException);

        private void HandleValues(object command, Guid requestId) {
            var c = command as IRequireConnectionId;
            if (c != null)
                c.ConnectionId = Context.ConnectionId;
            var r = command as IRequireRequestId;
            if (r != null)
                r.RequestId = requestId;
        }

        protected Task<Unit> DispatchNextAction(Guid requestId)
            => Cheat.Mediator.DispatchNextAction(SendAsync, requestId);

        public async Task Cancel(Guid requestId) => A.CancellationTokenMapping.Cancel(requestId);
    }
}