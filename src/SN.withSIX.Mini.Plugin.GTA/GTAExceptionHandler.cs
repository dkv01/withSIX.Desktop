// <copyright company="SIX Networks GmbH" file="GTAExceptionHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using ReactiveUI;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Plugin.GTA.Models;

namespace SN.withSIX.Mini.Plugin.GTA
{
    public class GTAExceptionHandler : BasicExternalExceptionhandler, IUsecaseExecutor
    {
        const string ScriptHook =
            @"We detected that you are missing ScriptHookV.
This Script mod is required to use mods with GTA5, you have to install it first.
ScriptHookV is currently available for manual download exclusively on the authors Homepage.

Please install it from: http://www.dev-c.com/gtav/scripthookv then press 'Retry'";
        const string OpenIv =
            @"We detected that you are missing OpenIV.
This mod is required to use mods with GTA5, you have to install it first.
OpenIV is currently available for manual download exclusively on the authors Homepage.

Please install it from: http://openiv.com then press 'Retry'";

        public override UserError HandleException(Exception ex, string action = "Action") {
            Contract.Requires<ArgumentNullException>(action != null);
            return Handle((dynamic) ex, action);
        }

        DependencyMissingUserError Handle(OpenIvMissingException ex, string action) {
            var webBrowserCommand = new NonRecoveryCommand("Get OpenIV");
            webBrowserCommand.MergeTask(
                () => this.RequestAsync(new OpenArbWebLink(new Uri("http://openiv.com")))).Subscribe();
            return new DependencyMissingUserError("Please install OpenIV",
                OpenIv,
                RecoveryCommandsImmediate.RetryCommands.Concat(new[] {webBrowserCommand}),
                innerException: ex);
        }

        DependencyMissingUserError Handle(ScriptHookMissingException ex, string action) {
            var webBrowserCommand = new NonRecoveryCommand("Get ScriptHookV");
            webBrowserCommand.MergeTask(
                () => this.RequestAsync(new OpenArbWebLink(new Uri("http://www.dev-c.com/gtav/scripthookv"))))
                .Subscribe();
            return new DependencyMissingUserError("Please install the latest version of ScriptHook", ScriptHook,
                RecoveryCommandsImmediate.RetryCommands.Concat(new[] {webBrowserCommand}), innerException: ex);
        }

        DependencyMissingUserError Handle(MultiGameRequirementMissingException ex, string action)
            => new DependencyMissingUserError("Please install the following components", GetMultiText(ex),
                RecoveryCommandsImmediate.RetryCommands.Concat(GetMultiCommands(ex)), innerException: ex);

        IEnumerable<IRecoveryCommand> GetMultiCommands(
            MultiGameRequirementMissingException multiGameRequirementMissingException) {
            foreach (var ex in multiGameRequirementMissingException.Exceptions) {
                if (ex is OpenIvMissingException) {
                    var webBrowserCommand = new NonRecoveryCommand("Get the latest OpenIV");
                    webBrowserCommand
                        .MergeTask(() => this.RequestAsync(new OpenArbWebLink(new Uri("http://openiv.com"))))
                        .Subscribe();
                    yield return webBrowserCommand;
                } else if (ex is ScriptHookMissingException) {
                    var scriptHookCommand = new NonRecoveryCommand("Get the latest ScriptHookV");
                    scriptHookCommand
                        .MergeTask(() =>
                            this.RequestAsync(
                                new OpenArbWebLink(new Uri("http://www.dev-c.com/gtav/scripthookv")))).Subscribe();
                    scriptHookCommand
                        .MergeTask(
                            () =>
                                this.RequestAsync(new OpenArbWebLink(new Uri("http://www.dev-c.com/gtav/scripthookv"))))
                        .Subscribe();
                    yield return scriptHookCommand;
                }
            }
        }

        static string GetMultiText(MultiGameRequirementMissingException multiGameRequirementMissingException) {
            var text = new List<string>();
            foreach (var ex in multiGameRequirementMissingException.Exceptions) {
                if (ex is OpenIvMissingException)
                    text.Add(OpenIv);
                else if (ex is ScriptHookMissingException)
                    text.Add(ScriptHook);
            }

            return string.Join("\n", text);
        }
    }

    public class DependencyMissingUserError : UserError
    {
        public DependencyMissingUserError(string errorMessage, string errorCauseOrResolution = null,
            IEnumerable<IRecoveryCommand> recoveryOptions = null, Dictionary<string, object> contextInfo = null,
            Exception innerException = null)
            : base(errorMessage, errorCauseOrResolution, recoveryOptions, contextInfo, innerException) {}
    }
}