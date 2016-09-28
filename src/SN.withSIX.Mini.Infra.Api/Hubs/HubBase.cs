// <copyright company="SIX Networks GmbH" file="HubBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNet.SignalR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;
using withSIX.Api.Models.Exceptions;

namespace SN.withSIX.Mini.Infra.Api.Hubs
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
            A.Excecutor.ApiAction(() => UsecaseExecutorExtensions.SendAsync(this, command), command,
                CreateException);

        // TODO: We need to actually Create a dictionary from the error data instead, so we can drop the crappy Serializing stuf
        private Exception CreateException(string msg, Exception inner)
            => new HubException(msg, (inner as UserException)?.GetObjectData());

        protected Task<TResponse> SendAsync<TResponse>(IAsyncRequest<TResponse> command)
            => A.Excecutor.ApiAction(() => UsecaseExecutorExtensions.SendAsync(this, command), command,
                CreateException);

        protected Task<TResponse> SendAsync<TResponse>(ICancellableQuery<TResponse> command, Guid requestId)
            => A.Excecutor.ApiAction(async () => {
                    var cancellationToken = A.CancellationTokenMapping.AddToken(requestId);
                    try {
                        return await this.SendAsync(command, cancellationToken).ConfigureAwait(false);
                    } finally {
                        A.CancellationTokenMapping.Remove(requestId);
                    }
                }, command,
                CreateException);

        protected Task<Unit> DispatchNextAction(Guid requestId)
            => Cheat.Mediator.DispatchNextAction(SendAsync, requestId);

        public async Task Cancel(Guid requestId) => A.CancellationTokenMapping.Cancel(requestId);
    }
}