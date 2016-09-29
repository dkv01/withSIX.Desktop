// <copyright company="SIX Networks GmbH" file="MediatorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;

namespace withSIX.Core.Applications.Extensions
{
    public static class MediatorExtensions
    {
        public static async Task<Unit> Void<T>(this Task<T> task) {
            await task.ConfigureAwait(false);
            return Unit.Value;
        }

        public static async Task<Unit> Void(this Task task) {
            await task.ConfigureAwait(false);
            return Unit.Value;
        }

        /// <summary>
        ///     Wrapped into a Task.Run, so that all processing of the command happens on the background thread.
        /// </summary>
        /// <typeparam name="TResponseData"></typeparam>
        /// <param name="mediator"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Task<TResponseData> RequestAsyncWrapped<TResponseData>(this IMediator mediator,
            IAsyncRequest<TResponseData> request) => Task.Run(() => mediator.SendAsync(request));

        public static Task<TResponseData> RequestAsync<TResponseData>(this IMediator mediator,
            ICompositeCommand<TResponseData> message) => message.Execute(mediator);

        public static Task<TResponseData> Execute<TResponseData>(this IAsyncRequest<TResponseData> message,
            IMediator mediator) => mediator.SendAsync(message);

        public static Task<TResponseData> Execute<TResponseData>(this ICancellableAsyncRequest<TResponseData> message,
            IMediator mediator, CancellationToken cancelToken) => mediator.SendAsync(message, cancelToken);
    }
}