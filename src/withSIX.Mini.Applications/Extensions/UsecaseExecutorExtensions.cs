// <copyright company="SIX Networks GmbH" file="UsecaseExecutorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Features;

namespace withSIX.Mini.Applications.Extensions
{
    public static class UsecaseExecutorExtensions
    {
        public static Task<TResponseData> Send<TResponseData>(this IUsecaseExecutor _,
            IRequest<TResponseData> message, CancellationToken cancelToken = default(CancellationToken))
            => message.ExecuteWrapped(cancelToken);

        public static Task Send(this IUsecaseExecutor _,
            IRequest message, CancellationToken cancelToken = default(CancellationToken))
            => message.ExecuteWrapped(cancelToken);


        public static Task DispatchNextAction(this IUsecaseExecutor executor, Guid requestId,
            CancellationToken cancelToken = default(CancellationToken))
            => Cheat.Mediator.DispatchNextAction((message, ct) => Send(executor, message, cancelToken), requestId);

        /*
        public static Task<T> OpenScreenAsync<T>(this IUsecaseExecutor executor, IAsyncQuery<T> query) where T : class, IScreenViewModel
            => query.OpenScreen();

        public static Task<T> OpenScreenCached<T>(this IUsecaseExecutor executor, IAsyncQuery<T> query) where T : class, IScreenViewModel
            => query.OpenScreenCached();
            */

        public static Task OpenWebLink(this IUsecaseExecutor executor, ViewType type, string additional = null)
            => executor.Send(new OpenWebLink(type, additional));
    }
}