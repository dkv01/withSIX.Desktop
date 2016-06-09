// <copyright company="SIX Networks GmbH" file="UserErrorBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using ReactiveUI;

namespace SN.withSIX.Core.Applications.Errors
{
    public class RestartAsAdministratorUserError : UserErrorBase
    {
        public static readonly IRecoveryCommand Restart = new RecoveryCommandImmediate("Restart as Administrator",
            o => RecoveryOptionResult.RetryOperation);

        public RestartAsAdministratorUserError(Dictionary<string, object> contextInfo = null,
            Exception innerException = null)
            : base(
                "The action requires admin rights, retry while running as administrator?",
                "It seems you don't have permission",
                new[] {Restart, RecoveryCommandImmediate.Cancel}, contextInfo, innerException) {}
    }

    public class NotConnectedUserError : UserErrorBase
    {
        public NotConnectedUserError(Dictionary<string, object> contextInfo = null, Exception innerException = null)
            : base(
                "The action requires connection to withSIX, retry?", "It seems you've lost connection to withSIX",
                RecoveryCommandsImmediate.RetryCommands, contextInfo, innerException) {}
    }

    public class NotLoggedInUserError : UserErrorBase
    {
        public NotLoggedInUserError(Dictionary<string, object> contextInfo = null, Exception innerException = null)
            : base(
                "The action requires login to withSIX, retry?", "It seems you're not logged in to withSIX",
                RecoveryCommandsImmediate.RetryCommands, contextInfo, innerException) {}
    }

    public class BusyUserError : UserErrorBase
    {
        public BusyUserError(Dictionary<string, object> contextInfo = null, Exception innerException = null)
            : base(
                "The system is currently busy. Retry?",
                "You cannot perform the selected action until the system is no longer busy.",
                RecoveryCommandsImmediate.RetryCommands, contextInfo, innerException) {}
    }

    public class CanceledUserError : UserErrorBase
    {
        public CanceledUserError(string errorMessage, string errorCauseOrResolution = null,
            IEnumerable<IRecoveryCommand> recoveryOptions = null, Dictionary<string, object> contextInfo = null,
            OperationCanceledException innerException = null)
            : base(errorMessage, errorCauseOrResolution, recoveryOptions, contextInfo, innerException) {}
    }
}