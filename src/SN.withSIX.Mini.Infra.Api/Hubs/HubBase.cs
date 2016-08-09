﻿// <copyright company="SIX Networks GmbH" file="HubBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;

namespace SN.withSIX.Mini.Infra.Api.Hubs
{
    public abstract class HubBase<T> : Hub<T>, IUsecaseExecutor where T : class
    {
        private readonly Excecutor _excecutor = new Excecutor();

        protected Task<TResponseData> SendAsync<TResponseData>(ICompositeCommand<TResponseData> command) =>
            _excecutor.ApiAction(() => UsecaseExecutorExtensions.SendAsync(this, command), command,
                CreateException);

        private Exception CreateException(string msg, Exception inner) => new HubException(msg, inner);

        protected Task<TResponse> SendAsync<TResponse>(IAsyncRequest<TResponse> command)
            =>
                _excecutor.ApiAction(() => UsecaseExecutorExtensions.SendAsync(this, command), command,
                    CreateException);

        protected Task<Unit> DispatchNextAction(Guid requestId)
            => Cheat.Mediator.DispatchNextAction(SendAsync, requestId);
    }
}