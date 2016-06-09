// <copyright company="SIX Networks GmbH" file="MediatorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using ShortBus;
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

        public static Task Notify<TMessage>(this TMessage message) where TMessage : class {
            Cheat.Mediator.Notify(message);
            return Cheat.Mediator.NotifyAsync(message);
        }

        public static async Task Raise<TMessage>(this TMessage message) where TMessage : class, IDomainEvent {
            await message.Notify().ConfigureAwait(false);
            Cheat.MessageBus.SendMessage(message);
        }

        /*
        public static Task<T> OpenScreen<T>(this IAsyncQuery<T> query) where T : class, IScreenViewModel
            => Cheat.ScreenOpener.OpenAsyncQuery(query);

        public static Task<T> OpenScreenCached<T>(this IAsyncQuery<T> query) where T : class, IScreenViewModel
            => Cheat.ScreenOpener.OpenAsyncQueryCached(query);
            */
    }
}