// <copyright company="SIX Networks GmbH" file="MediatorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Core.Applications.Extensions
{
    public static class MediatorExtensions
    {
        public static async Task<UnitType> Void<T>(this Task<T> task) {
            await task.ConfigureAwait(false);
            return UnitType.Default;
        }

        public static async Task<UnitType> Void(this Task task) {
            await task.ConfigureAwait(false);
            return UnitType.Default;
        }

        /// <summary>
        ///     Wrapped into a Task.Run, so that all processing of the command happens on the background thread.
        /// </summary>
        /// <typeparam name="TResponseData"></typeparam>
        /// <param name="mediator"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Task<TResponseData> RequestAsyncWrapped<TResponseData>(this IMediator mediator,
            IAsyncRequest<TResponseData> request) => Task.Run(() => mediator.RequestAsync(request));

        public static Task<TResponseData> RequestAsync<TResponseData>(this IMediator mediator,
            ICompositeCommand<TResponseData> message) => message.Execute(mediator);

        public static Task<TResponseData> Execute<TResponseData>(this IAsyncRequest<TResponseData> message,
            IMediator mediator) => mediator.RequestAsync(message);
    }
}