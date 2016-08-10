// <copyright company="SIX Networks GmbH" file="MediatorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Mini.Applications.Extensions
{
    public static class MediatorExtensions
    {
        public static Task<TResponseData> ExecuteWrapped<TResponseData>(this ICompositeCommand<TResponseData> message)
            => Task.Run(message.Execute);

        public static Task<TResponseData> ExecuteWrapped<TResponseData>(this IAsyncRequest<TResponseData> message)
            => Task.Run(message.Execute);

        public static Task<TResponseData> Execute<TResponseData>(this ICompositeCommand<TResponseData> message)
            => message.Execute(Cheat.Mediator);

        public static Task<TResponseData> Execute<TResponseData>(this IAsyncRequest<TResponseData> message)
            => message.Execute(Cheat.Mediator);

        // We are using dynamic here because we don't want to care about Async or Sync handlers for notifications
        public static Task Publish(this IDomainEvent message) => PublishEvent((dynamic) message);

        static Task PublishEvent(IAsyncNotification evt) => Cheat.Mediator.PublishAsync(evt);
        static async Task PublishEvent(INotification evt) => Cheat.Mediator.Publish(evt);

        public static async Task Raise(this IDomainEvent message) {
            await message.Publish().ConfigureAwait(false);
            Cheat.MessageBus.SendMessage((dynamic)message);
        }

        /*
        public static Task<T> OpenScreen<T>(this IAsyncQuery<T> query) where T : class, IScreenViewModel
            => Cheat.ScreenOpener.OpenAsyncQuery(query);

        public static Task<T> OpenScreenCached<T>(this IAsyncQuery<T> query) where T : class, IScreenViewModel
            => Cheat.ScreenOpener.OpenAsyncQueryCached(query);
            */
    }
}