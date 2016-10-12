// <copyright company="SIX Networks GmbH" file="HubBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using withSIX.Api.Models.Exceptions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Infra.Services;
using withSIX.Core.Presentation;
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

        public static async Task<TResponse> ApiAction<TResponse>(Func<CancellationToken, Task<TResponse>> action, object command,
            Func<string, Exception, Exception> createException, Guid requestId, string connectionId, IPrincipal user,
            IRequestScopeService scope) {
            var ct = CancellationTokenMapping.AddToken(requestId);
            try {
                using (scope.StartScope(connectionId, requestId, user, ct))
                    return await Excecutor.ApiAction(() => action(ct), command, createException).ConfigureAwait(false);
            } finally {
                CancellationTokenMapping.Remove(requestId);
            }
        }
    }

    public abstract class HubBase<T> : Hub<T>, IUsecaseExecutor where T : class
    {
        protected Task<TResponseData> SendAsync<TResponseData>(ICompositeCommand<TResponseData> command) {
            var requestId = Guid.NewGuid();

            return A.ApiAction(ct => UsecaseExecutorExtensions.SendAsync(this, command), command,
                CreateException, requestId, Context.ConnectionId, Context.User, RequestScopeService.Instance);
        }

        // TODO: We need to actually Create a dictionary from the error data instead, so we can drop the crappy Serializing stuf
        private Exception CreateException(string msg, Exception inner)
            => new HubException(msg, (inner as UserException)?.GetObjectData());

        protected Task<TResponse> SendAsync<TResponse>(IAsyncRequest<TResponse> command) {
            var requestId = Guid.NewGuid();
            return A.ApiAction(
                ct => UsecaseExecutorExtensions.SendAsync(this, command), command,
                CreateException, requestId, Context.ConnectionId, Context.User, RequestScopeService.Instance);
        }

        protected Task<TResponse> SendAsync<TResponse>(ICancellableQuery<TResponse> command, Guid requestId)
            => A.ApiAction(async ct => await this.SendAsync(command, ct).ConfigureAwait(false), command,
                CreateException, requestId, Context.ConnectionId, Context.User, RequestScopeService.Instance);

        protected Task<Unit> DispatchNextAction(Guid requestId)
            => Cheat.Mediator.DispatchNextAction(SendAsync, requestId);

        public async Task Cancel(Guid requestId) => A.CancellationTokenMapping.Cancel(requestId);
    }
}