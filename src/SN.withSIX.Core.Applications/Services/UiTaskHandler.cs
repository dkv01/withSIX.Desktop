// <copyright company="SIX Networks GmbH" file="UiTaskHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using ReactiveUI;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.Core.Applications.Services
{
    public static class UiTaskHandler
    {
        public static IScheduler Scheduler = TaskPoolScheduler.Default;
        public static Action<IReactiveCommand, string> RegisterCommand;
        public static ReactiveCommand<Unit> CreateAsyncVoidTask(Func<Task> task)
            => ReactiveCommand.CreateAsyncTask(async x => await task().ConfigureAwait(false));

        public static TCommand DefaultSetup<TCommand>(this TCommand command, string name)
            where TCommand : IReactiveCommand {
            SetupDefaultThrownExceptionsHandler(command, name);
            command.Setup(name);
            return command;
        }

        static void SetupDefaultThrownExceptionsHandler<TCommand>(TCommand command, string action)
            where TCommand : IReactiveCommand {
            RegisterCommand(command, action);
        }

        static TCommand Setup<TCommand>(this TCommand command, string name) where TCommand : IReactiveCommand {
            if (Common.Flags.Verbose)
                SetupBencher(command, name);
            return command;
        }

        static void SetupBencher(IReactiveCommand command, string name = null) {
            Bencher bencher = null;
            IDisposable subscription = null;
            subscription = command.IsExecuting.Subscribe(x => HandleBencher(x, ref bencher, name, subscription));
        }

        static void HandleBencher(bool b, ref Bencher bencher, string name, IDisposable subscription) {
            if (b)
                bencher = new Bencher("UICommand", name ?? "TODO");
            else {
                bencher?.Dispose();
                subscription?.Dispose();
            }
        }
    }
}