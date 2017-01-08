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
using withSIX.Core;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Services;

namespace withSIX.Mini.Applications
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

        public Task DispatchNextAction(Func<IVoidCommand, CancellationToken, Task> dispatcher, Guid requestId)
            => _stateHandler.DispatchNextAction(dispatcher, requestId, CancellationToken.None);

        public Task<TResponseData> Send<TResponseData>(IRequest<TResponseData> request, CancellationToken cancelToken = default(CancellationToken))
            => _mediator.Send(request, cancelToken);

        public Task Send(IRequest request, CancellationToken cancelToken = default(CancellationToken))
            => _mediator.Send(request, cancelToken);

        public Task Publish<TNotification>(TNotification notification,
            CancellationToken cancelToken = default(CancellationToken)) where TNotification : INotification
            => _mediator.Publish(notification, cancelToken);
    }

    public interface IActionDispatcher : IMediator
    {
        Task DispatchNextAction(Func<IVoidCommand, CancellationToken, Task> dispatcher, Guid requestId);
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
        public static string ReleaseTitle { get; } = GetReleaseTitle();

        private static string GetReleaseTitle() {
            switch (BuildFlags.Type) {
            case ReleaseType.Stable:
                return null;
            default: {
                return BuildFlags.Type.ToString().ToUpper();
            }
            }
        }

        public static string DirectorySubtitle { get; } = GetDirectorySubtitle();
        private static string GetDirectorySubtitle() {
            switch (BuildFlags.Type) {
            case ReleaseType.Beta:
            case ReleaseType.Stable:
                return null;
            default: {
                return BuildFlags.Type.ToString().ToLower();
            }
            }
        }

        public static string DirectoryTitle { get; } = ProductTitle + (DirectorySubtitle == null ? null : "-" + DirectorySubtitle);
        public static string DisplayTitle { get; } = ProductTitle + (ReleaseTitle == null ? null : " " + ReleaseTitle);
        public static string WindowTitle { get; } = DisplayTitle;
        public const int DefaultHttpPort = 48665; // TODO: Randomize and make dynamic on first start
        public const int DefaultHttpsPort = 48666; // TODO: Randomize and make dynamic on first start
        public static int ApiPort { get; set; }
        public static int ApiHttpPort { get; set; }
        private static readonly IPAddress srvAddress = IPAddress.Parse("127.0.0.66");
        private static readonly Lazy<IPEndPoint> httpsAddress =
            new Lazy<IPEndPoint>(() => ApiPort == 0 ? null : new IPEndPoint(srvAddress, ApiPort));
        public static IPEndPoint HttpsAddress => httpsAddress.Value;
        private static readonly Lazy<IPEndPoint> httpAddress =
            new Lazy<IPEndPoint>(() => ApiHttpPort == 0 ? null : new IPEndPoint(srvAddress, ApiHttpPort));
        public static IPEndPoint HttpAddress => httpAddress.Value;

        public const int SyncVersion = 14;

        public static string InternalVersion { get; set; }
        public static string ProductVersion { get; set; }
        public static bool IsTestVersion { get; } = GetIsTestVersion();

        private static bool GetIsTestVersion() {
#if DEBUG
        return true;
#else
            return BuildFlags.Type <= ReleaseType.Alpha;
#endif
        }

        // TODO: Consider FirstRun not just from Setup but also in terms of Settings.... so that deleting settings is a new FirstRun?
        public static bool FirstRun { get; set; }
        public static string ApiVersion { get; } = "4";
        public static Browser PluginBrowserFound { get; set; }
        public static string CertThumb { get; } = "fca9282c0cd0394f61429bbbfdb59bacfc7338c9";

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