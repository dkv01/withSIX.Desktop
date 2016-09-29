﻿// <copyright company="SIX Networks GmbH" file="MediatorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;

namespace withSIX.Mini.Applications.Extensions
{
    public static class MediatorExtensions
    {
        public static Task<TResponseData> ExecuteWrapped<TResponseData>(this ICompositeCommand<TResponseData> message)
            => Task.Run(message.Execute);

        public static Task<TResponseData> ExecuteWrapped<TResponseData>(this IAsyncRequest<TResponseData> message)
            => Task.Run(message.Execute);

        public static Task<TResponseData> ExecuteWrapped<TResponseData>(
                this ICancellableAsyncRequest<TResponseData> message, CancellationToken cancelToken)
            => Task.Run(() => message.Execute(cancelToken));

        public static Task<TResponseData> Execute<TResponseData>(this ICompositeCommand<TResponseData> message)
            => message.Execute(Cheat.Mediator);

        public static Task<TResponseData> Execute<TResponseData>(this IAsyncRequest<TResponseData> message)
            => message.Execute(Cheat.Mediator);

        public static Task<TResponseData> Execute<TResponseData>(this ICancellableAsyncRequest<TResponseData> message,
                CancellationToken cancelToken)
            => message.Execute(Cheat.Mediator, cancelToken);

        // We are using dynamic here because we don't want to care about Async or Sync handlers for notifications
        static Task PublishToMediatorDynamically(this IDomainEvent message) => PublishToMediator((dynamic) message);
        static Task PublishToMediator(IAsyncNotification evt) => Cheat.Mediator.PublishAsync(evt);
        static async Task PublishToMediator(INotification evt) => Cheat.Mediator.Publish(evt);

        public static async Task Raise(this IDomainEvent message) {
            await message.PublishToMediatorDynamically().ConfigureAwait(false);
            message.PublishToMessageBusDynamically();
        }
    }
}