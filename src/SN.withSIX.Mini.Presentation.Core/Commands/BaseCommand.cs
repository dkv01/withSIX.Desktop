// <copyright company="SIX Networks GmbH" file="BaseCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using ManyConsole;
using SN.withSIX.Core.Logging;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Presentation.Core.Commands
{
    public abstract class BaseCommand : ConsoleCommand, IEnableLogging
    {
        protected void HasFlag(string flags, string desc, Action<bool> action)
            => HasOption(flags, desc, s => action(s != null));
    }

    public abstract class BaseCommandAsync : BaseCommand
    {
        public override sealed int Run(string[] remainingArguments) => RunAsync(remainingArguments).WaitAndUnwrapException();

        protected abstract Task<int> RunAsync(string[] remainingArguments);
    }
}