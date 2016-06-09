// <copyright company="SIX Networks GmbH" file="UsecaseExecutorExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases;

namespace SN.withSIX.Mini.Applications.Extensions
{
    public interface IUsecaseExecutor {}

    public static class UsecaseExecutorExtensions
    {
        public static Task<TResponseData> RequestAsync<TResponseData>(this IUsecaseExecutor _,
            ICompositeCommand<TResponseData> message) => message.ExecuteWrapped();

        public static Task<TResponseData> RequestAsync<TResponseData>(this IUsecaseExecutor _,
            IAsyncRequest<TResponseData> message) => message.ExecuteWrapped();

        public static Task<UnitType> DispatchNextAction(this IUsecaseExecutor executor, Guid requestId)
            => Cheat.Mediator.DispatchNextAction(message => RequestAsync(executor, message), requestId);

        public static IObservable<T> Listen<T>(this IUsecaseExecutor _) => Cheat.MessageBus.Listen<T>();

        public static IObservable<T> ListenIncludeLatest<T>(this IUsecaseExecutor _)
            => Cheat.MessageBus.ListenIncludeLatest<T>();

        /*
        public static Task<T> OpenScreenAsync<T>(this IUsecaseExecutor executor, IAsyncQuery<T> query) where T : class, IScreenViewModel
            => query.OpenScreen();

        public static Task<T> OpenScreenCached<T>(this IUsecaseExecutor executor, IAsyncQuery<T> query) where T : class, IScreenViewModel
            => query.OpenScreenCached();
            */

        public static Task OpenWebLink(this IUsecaseExecutor executor, ViewType type, string additional = null)
            => executor.RequestAsync(new OpenWebLink(type, additional));
    }
}