// <copyright company="SIX Networks GmbH" file="MediatorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace withSIX.Core.Applications.Extensions
{
    public static class MediatorExtensions
    {
        [Obsolete]
        public static async Task<Unit> Void<T>(this Task<T> task) {
            await task.ConfigureAwait(false);
            return Unit.Value;
        }

        [Obsolete]
        public static async Task<Unit> Void(this Task task) {
            await task.ConfigureAwait(false);
            return Unit.Value;
        }

        public static async Task<object> VoidObject(this Task task)
        {
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
            IRequest<TResponseData> request, CancellationToken cancelToken = default(CancellationToken)) {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (mediator == null) throw new ArgumentNullException(nameof(mediator));
            return Task.Run(() => mediator.Send(request, cancelToken), cancelToken);
        }

        public static Task<TResponseData> Execute<TResponseData>(this IRequest<TResponseData> message,
            IMediator mediator, CancellationToken cancelToken = default(CancellationToken)) {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (mediator == null) throw new ArgumentNullException(nameof(mediator));
            return mediator.Send(message, cancelToken);
        }


        public static Task RequestAsyncWrapped(this IMediator mediator,
            IRequest request, CancellationToken cancelToken = default(CancellationToken)) {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (mediator == null) throw new ArgumentNullException(nameof(mediator));
            return Task.Run(() => mediator.Send(request, cancelToken), cancelToken);
        }

        public static Task Execute(this IRequest message,
            IMediator mediator, CancellationToken cancelToken = default(CancellationToken)) {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (mediator == null) throw new ArgumentNullException(nameof(mediator));
            return mediator.Send(message, cancelToken);
        }
    }
}