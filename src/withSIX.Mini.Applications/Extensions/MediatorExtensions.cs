// <copyright company="SIX Networks GmbH" file="MediatorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core;
using withSIX.Core.Applications.Extensions;

namespace withSIX.Mini.Applications.Extensions
{
    public static class MediatorExtensions
    {
        public static Task<TResponseData> ExecuteWrapped<TResponseData>(this IRequest<TResponseData> message,
            CancellationToken cancelToken = default(CancellationToken))
            => Task.Run(() => message.Execute(cancelToken));

        public static Task<TResponseData> Execute<TResponseData>(this IRequest<TResponseData> message,
            CancellationToken cancelToken = default(CancellationToken))
            => message.Execute(Cheat.Mediator, cancelToken);

        public static Task ExecuteWrapped(this IRequest message,
            CancellationToken cancelToken = default(CancellationToken))
            => Task.Run(() => message.Execute(cancelToken));

        public static Task Execute(this IRequest message, CancellationToken cancelToken = default(CancellationToken))
            => message.Execute(Cheat.Mediator, cancelToken);

        [Obsolete]
        static Task PublishToMediatorDynamically(this IDomainEvent message) => PublishToMediator(message);

        static Task PublishToMediator(INotification evt) => Cheat.Mediator.Publish(evt);

        public static async Task Raise(this IDomainEvent message) {
            await message.PublishToMediatorDynamically().ConfigureAwait(false);
            message.PublishToMessageBusDynamically();
        }
    }
}