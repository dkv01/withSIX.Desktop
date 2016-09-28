// <copyright company="SIX Networks GmbH" file="UserErrorHandling.cs">
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
using SN.withSIX.Core.Applications.Errors;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Core.Applications.Services
{
    public static class UserErrorHandling
    {
        public static void Setup() {
            UserErrorHandler.HandleUserError = HandleUserError;
        }

        private static async Task<RecoveryOptionResultModel> HandleUserError(this UserErrorModel informationalUserError) {
            var r = await UserError.Throw(informationalUserError.MapTo<UserError>());
            return ConvertOption(r);
        }

        public static RecoveryOptionResultModel ConvertOption(this RecoveryOptionResult r)
            => (RecoveryOptionResultModel) Enum.Parse(typeof(RecoveryOptionResultModel), r.ToString());

        public static RecoveryOptionResult ConvertOption(this RecoveryOptionResultModel r)
            => (RecoveryOptionResult) Enum.Parse(typeof(RecoveryOptionResult), r.ToString());
    }

    public static class UserErrorExtensions
    {
        public static Task<RecoveryOptionResult> GetTask(this IReadOnlyCollection<IRecoveryCommand> commands)
            => GetTask2<RecoveryCommand>(commands).Concat(GetTask2<RecoveryCommandImmediate>(commands))
                .Merge()
                .Select(x => x.GetValueOrDefault(RecoveryOptionResult.FailOperation))
                .FirstAsync()
                .ToTask();

        static IEnumerable<IObservable<RecoveryOptionResult?>> GetTask2<T>(IEnumerable<IRecoveryCommand> commands)
            where T : IRecoveryCommand, IObservable<Unit> => commands.OfType<T>()
            .Select(x => x.Select(_ => x.RecoveryResult));
    }

    public class RecoveryCommandImmediate : ReactiveCommand<Unit>, IRecoveryCommand
    {
        /// <summary>
        ///     Constructs a RecoveryCommand.
        /// </summary>
        /// <param name="commandName">The user-visible name of this Command.</param>
        /// <param name="handler">
        ///     A convenience handler - equivalent to
        ///     Subscribing to the command and setting the RecoveryResult.
        /// </param>
        public RecoveryCommandImmediate(string commandName, Func<object, RecoveryOptionResult> handler = null)
            : base(Observable.Return(true), _ => Observable.Return(Unit.Default), Scheduler.Immediate) {
            CommandName = commandName;

            if (handler != null) {
                this.Subscribe(x => RecoveryResult = handler(x));
            }
        }

        public bool IsDefault { get; set; }
        public bool IsCancel { get; set; }

        /// <summary>
        ///     A default command whose caption is "Ok"
        /// </summary>
        /// <value>RetryOperation</value>
        public static IRecoveryCommand Ok
        {
            get
            {
                var ret = new RecoveryCommandImmediate("Ok") {IsDefault = true};
                ret.Subscribe(_ => ret.RecoveryResult = RecoveryOptionResult.RetryOperation);
                return ret;
            }
        }

        /// <summary>
        ///     A default command whose caption is "Continue"
        /// </summary>
        /// <value>RetryOperation</value>
        public static IRecoveryCommand Continue
        {
            get
            {
                var ret = new RecoveryCommandImmediate("Continue") {IsDefault = true};
                ret.Subscribe(_ => ret.RecoveryResult = RecoveryOptionResult.RetryOperation);
                return ret;
            }
        }

        /// <summary>
        ///     A default command whose caption is "Cancel"
        /// </summary>
        /// <value>FailOperation</value>
        public static IRecoveryCommand Cancel
        {
            get
            {
                var ret = new RecoveryCommandImmediate("Cancel") {IsCancel = true};
                ret.Subscribe(_ => ret.RecoveryResult = RecoveryOptionResult.CancelOperation);
                return ret;
            }
        }

        /// <summary>
        ///     A default command whose caption is "Yes"
        /// </summary>
        /// <value>RetryOperation</value>
        public static IRecoveryCommand Yes
        {
            get
            {
                var ret = new RecoveryCommandImmediate("Yes") {IsDefault = true};
                ret.Subscribe(_ => ret.RecoveryResult = RecoveryOptionResult.RetryOperation);
                return ret;
            }
        }

        /// <summary>
        ///     A default command whose caption is "No"
        /// </summary>
        /// <value>FailOperation</value>
        public static IRecoveryCommand No
        {
            get
            {
                var ret = new RecoveryCommandImmediate("No") {IsCancel = true};
                ret.Subscribe(_ => ret.RecoveryResult = RecoveryOptionResult.CancelOperation);
                return ret;
            }
        }
        public string CommandName { get; protected set; }
        public RecoveryOptionResult? RecoveryResult { get; set; }
    }
}