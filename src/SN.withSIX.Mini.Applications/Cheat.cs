// <copyright company="SIX Networks GmbH" file="Cheat.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ReactiveUI;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases.Main;

namespace SN.withSIX.Mini.Applications
{
    // Stateful service (stateHandler)
    public class ActionDispatcher : IActionDispatcher, IApplicationService
    {
        private readonly IMediator _mediator;
        private readonly IStateHandler _stateHandler;

        public ActionDispatcher(IMediator mediator, IStateHandler stateHandler) {
            _mediator = mediator;
            _stateHandler = stateHandler;
        }

        public Task<Unit> DispatchNextAction(Func<IAsyncVoidCommand, Task<Unit>> dispatcher, Guid requestId)
            => _stateHandler.DispatchNextAction(dispatcher, requestId);

        public TResponseData Send<TResponseData>(IRequest<TResponseData> request) => _mediator.Send(request);

        public Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request)
            => _mediator.SendAsync(request);

        public Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) => _mediator.SendAsync(request, cancellationToken);

        public void Publish(INotification notification) => _mediator.Publish(notification);

        public Task PublishAsync(IAsyncNotification notification) => _mediator.PublishAsync(notification);

        public Task PublishAsync(ICancellableAsyncNotification notification, CancellationToken cancellationToken)
            => _mediator.PublishAsync(notification, cancellationToken);
    }

    public interface IActionDispatcher : IMediator
    {
        Task<Unit> DispatchNextAction(Func<IAsyncVoidCommand, Task<Unit>> dispatcher, Guid requestId);
    }

    public class ArgsO
    {
        //public bool Dynamic { get; set; }
        public int? Port { get; set; }
        public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();
    }

    /// <summary>
    ///     Globally accessible services and constants in the application layer
    ///     Cheat - Under advisement for better forms of access...
    /// </summary>
    public static class Cheat
    {
        static ICheatImpl _cheat;
        public static IActionDispatcher Mediator => _cheat.Mediator;
        public static bool IsShuttingDown => Common.Flags.ShuttingDown;
        public static ArgsO Args { get; set; } = new ArgsO();

        public static IMessageBus MessageBus => _cheat.MessageBus;

        public static void SetServices(ICheatImpl cheat) {
            if (_cheat != null)
                throw new NotSupportedException("Not supposed to set this twice");
            _cheat = cheat;
        }

        public static string WindowDisplayName(string title) => title; //+ " - " + Consts.ProductTitle;

        public static IObservable<T> Listen<T>(this IUsecaseExecutor _) => MessageBus.Listen<T>();

        public static IObservable<T> ListenIncludeLatest<T>(this IUsecaseExecutor _)
            => MessageBus.ListenIncludeLatest<T>();

        public static void PublishToMessageBus<T>(this T message) => MessageBus.SendMessage(message);
        // We are using dynamic here because the messagebus relies on generic typing
        public static void PublishToMessageBusDynamically(this IDomainEvent message)
            => MessageBus.SendMessage((dynamic) message);
    }

    public class BackgroundTasks : IDisposable
    {
        ConcurrentBag<Task> _tasks = new ConcurrentBag<Task>();

        public void Dispose() {
            var tasks = _tasks;
            _tasks = null;
            //foreach (var t in tasks)
            //  t.Dispose();
        }

        public void RegisterTask(Task task) {
            _tasks.Add(task);
        }


        public Task Await() => Task.WhenAll(_tasks);
    }

    public static class Consts
    {
        public const string InternalTitle = "Sync";
        public const string ProductTitle = InternalTitle;
        public const string ReleaseTitle =
#if NIGHTLY_RELEASE
                "ALPHA";
#else
#if BETA_RELEASE
                "BETA";
#else
#if MAIN_RELEASE
                null;
#else
            "DEV";
#endif
#endif
#endif
        public const string DirectorySubtitle =
#if NIGHTLY_RELEASE
                "alpha";
#else
#if BETA_RELEASE
                null;
#else
#if MAIN_RELEASE
                null;
#else
            "dev";
#endif
#endif
#endif
        public const string DirectoryTitle =
            ProductTitle + (DirectorySubtitle == null ? null : "-" + DirectorySubtitle);
        public const string DisplayTitle = ProductTitle + (ReleaseTitle == null ? null : " " + ReleaseTitle);
        public const string WindowTitle = DisplayTitle;
        public const int DefaultHttpsPort = 48666; // TODO: Randomize and make dynamic on first start
        public static int ApiPort { get; set; }
        private static readonly IPAddress srvAddress = IPAddress.Parse("127.0.0.66");
        public static IPEndPoint HttpAddress = null; // new IPEndPoint(SrvAddress, HttpPort);
        private static readonly Lazy<IPEndPoint> httpsAddress =
            new Lazy<IPEndPoint>(() => new IPEndPoint(srvAddress, ApiPort));
        public static IPEndPoint HttpsAddress => httpsAddress.Value;
        public const int SyncVersion = 14;

        public static string InternalVersion { get; set; }
        public static string ProductVersion { get; set; }
        public static bool IsTestVersion { get; }
#if DEBUG || NIGHTLY_RELEASE
        = true;
#else
            = false;
#endif
        // TODO: Consider FirstRun not just from Setup but also in terms of Settings.... so that deleting settings is a new FirstRun?
        public static bool FirstRun { get; set; }
        public static string ApiVersion { get; } = "4";
        public static Browser PluginBrowserFound { get; set; }

        public static class Features
        {
            public static bool Queue => IsTestVersion;
            public static bool UnreleasedGames => IsTestVersion;
        }
    }

    public interface ICheatImpl
    {
        IActionDispatcher Mediator { get; }
        IMessageBus MessageBus { get; }
    }

    public class CheatImpl : ICheatImpl, IApplicationService
    {
        public CheatImpl(IActionDispatcher mediator, IExceptionHandler exceptionHandler, IMessageBus messageBus) {
            Mediator = mediator;
            MessageBus = messageBus;
            ErrorHandlerr.SetExceptionHandler(exceptionHandler);
        }

        public IActionDispatcher Mediator { get; }
        public IMessageBus MessageBus { get; }
    }
}