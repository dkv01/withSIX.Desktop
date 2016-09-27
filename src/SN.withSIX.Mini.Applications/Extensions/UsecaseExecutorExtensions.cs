// <copyright company="SIX Networks GmbH" file="UsecaseExecutorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases;

namespace SN.withSIX.Mini.Applications.Extensions
{
    public static class UsecaseExecutorExtensions
    {
        public static Task<TResponseData> SendAsync<TResponseData>(this IUsecaseExecutor _,
            ICompositeCommand<TResponseData> message) => message.ExecuteWrapped();

        public static Task<TResponseData> SendAsync<TResponseData>(this IUsecaseExecutor _,
            IAsyncRequest<TResponseData> message) => message.ExecuteWrapped();

        public static Task<TResponseData> SendAsync<TResponseData>(this IUsecaseExecutor _,
                ICancellableAsyncRequest<TResponseData> message, CancellationToken cancelToken)
            => message.ExecuteWrapped(cancelToken);

        public static Task<Unit> DispatchNextAction(this IUsecaseExecutor executor, Guid requestId)
            => Cheat.Mediator.DispatchNextAction(message => SendAsync(executor, message), requestId);

        /*
        public static Task<T> OpenScreenAsync<T>(this IUsecaseExecutor executor, IAsyncQuery<T> query) where T : class, IScreenViewModel
            => query.OpenScreen();

        public static Task<T> OpenScreenCached<T>(this IUsecaseExecutor executor, IAsyncQuery<T> query) where T : class, IScreenViewModel
            => query.OpenScreenCached();
            */

        public static Task OpenWebLink(this IUsecaseExecutor executor, ViewType type, string additional = null)
            => executor.SendAsync(new OpenWebLink(type, additional));
    }
}