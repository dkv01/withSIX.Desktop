// <copyright company="SIX Networks GmbH" file="InformationalUserError.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using ReactiveUI;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.Core.Applications.Errors
{
    public interface IRxClose
    {
        ReactiveCommand<bool?> Close { get; set; }
    }

    public class NonRecoveryCommand : RecoveryCommandImmediate, IDontRecover
    {
        public NonRecoveryCommand(string commandName) : base(commandName) {}
    }

    /*
public static class RecoveryCommands
{
    public static readonly IRecoveryCommand> retry = new RecoveryCommand("Retry",
        o => RecoveryOptionResult.RetryOperation);
    public static IRecoveryCommand[] YesNoCommands = {RecoveryCommand.Yes, RecoveryCommand.No};
    public static IRecoveryCommand[] RetryCommands = {Retry, RecoveryCommand.Cancel};
}
*/

    public static class RecoveryCommandsImmediate
    {
        public static readonly IRecoveryCommand Retry = new RecoveryCommandImmediate("Retry", o => RecoveryOptionResult.RetryOperation);
        public static IRecoveryCommand[] YesNoCommands = { RecoveryCommandImmediate.Yes, RecoveryCommandImmediate.No };
        public static IRecoveryCommand[] RetryCommands = { Retry, RecoveryCommandImmediate.Cancel };
    }

    public class RecoveryCommandImmediate : ReactiveCommand<Unit>, IRecoveryCommand
    {
        public bool IsDefault { get; set; }
        public bool IsCancel { get; set; }
        public string CommandName { get; protected set; }
        public RecoveryOptionResult? RecoveryResult { get; set; }

        public static Task<RecoveryOptionResult> GetTask(
            IReadOnlyCollection<IRecoveryCommand> commands)
            => GetTask2<RecoveryCommand>(commands).Concat(GetTask2<RecoveryCommandImmediate>(commands))
                .Merge()
                .Select(x => x.GetValueOrDefault(RecoveryOptionResult.FailOperation))
                .FirstAsync()
                .ToTask();

        static IEnumerable<IObservable<RecoveryOptionResult?>> GetTask2<T>(IEnumerable<IRecoveryCommand> commands)
            where T : IRecoveryCommand, IObservable<Unit> => commands.OfType<T>()
                .Select(x => x.Select(_ => x.RecoveryResult));
        
        /// <summary>
        /// Constructs a RecoveryCommand.
        /// </summary>
        /// <param name="commandName">The user-visible name of this Command.</param>
        /// <param name="handler">A convenience handler - equivalent to
        /// Subscribing to the command and setting the RecoveryResult.</param>
        public RecoveryCommandImmediate(string commandName, Func<object, RecoveryOptionResult> handler = null)
            : base(Observable.Return(true), _ => Observable.Return(Unit.Default), Scheduler.Immediate) {
            CommandName = commandName;

            if (handler != null) {
                this.Subscribe(x => RecoveryResult = handler(x));
            }
        }

        /// <summary>
        /// A default command whose caption is "Ok"
        /// </summary>
        /// <value>RetryOperation</value>
        public static IRecoveryCommand Ok
        {
            get { var ret = new RecoveryCommandImmediate("Ok") { IsDefault = true }; ret.Subscribe(_ => ret.RecoveryResult = RecoveryOptionResult.RetryOperation); return ret; }
        }

        /// <summary>
        /// A default command whose caption is "Cancel"
        /// </summary>
        /// <value>FailOperation</value>
        public static IRecoveryCommand Cancel
        {
            get { var ret = new RecoveryCommandImmediate("Cancel") { IsCancel = true }; ret.Subscribe(_ => ret.RecoveryResult = RecoveryOptionResult.CancelOperation); return ret; }
        }

        /// <summary>
        /// A default command whose caption is "Yes"
        /// </summary>
        /// <value>RetryOperation</value>
        public static IRecoveryCommand Yes
        {
            get { var ret = new RecoveryCommandImmediate("Yes") { IsDefault = true }; ret.Subscribe(_ => ret.RecoveryResult = RecoveryOptionResult.RetryOperation); return ret; }
        }

        /// <summary>
        /// A default command whose caption is "No"
        /// </summary>
        /// <value>FailOperation</value>
        public static IRecoveryCommand No
        {
            get { var ret = new RecoveryCommandImmediate("No") { IsCancel = true }; ret.Subscribe(_ => ret.RecoveryResult = RecoveryOptionResult.CancelOperation); return ret; }
        }
    }


    public interface IDontRecover {}

    public class BasicUserError : UserError
    {
        public BasicUserError(string errorMessage, string errorCauseOrResolution = null, Dictionary<string, object> contextInfo = null,
            Exception innerException = null)
            : base(errorMessage, errorCauseOrResolution, new [] {RecoveryCommandImmediate.Cancel}, contextInfo, innerException) {}
    }

    public class RecoverableUserError : UserError
    {
        public RecoverableUserError(Exception innerException, string errorMessage, string errorCauseOrResolution = null, Dictionary<string, object> contextInfo = null)
            : base(errorMessage, errorCauseOrResolution, RecoveryCommandsImmediate.RetryCommands, contextInfo, innerException) { }
    }

    public class InformationalUserError : BasicUserError
    {
        public InformationalUserError(Exception exception, string title, string message)
            : base(
                title ?? "Non fatal error occurred", message + "\n\nError Info: " + exception.Message,
                null, exception) {
            // TODO: Temp log here... because we are loosing it otherwise ..
            MainLog.Logger.FormattedWarnException(exception);
        }
    }

    public class UsernamePasswordUserError : RecoverableUserError
    {
        public UsernamePasswordUserError(Exception innerException, string errorMessage,
            string errorCauseOrResolution = null,
            Dictionary<string, object> contextInfo = null)
            : base(innerException, errorMessage, errorCauseOrResolution, contextInfo) {}
    }

    public class InputUserError : RecoverableUserError
    {
        public InputUserError(Exception innerException, string errorMessage,
            string errorCauseOrResolution = null,
            Dictionary<string, object> contextInfo = null)
            : base(innerException, errorMessage, errorCauseOrResolution, contextInfo) { }
    }
}