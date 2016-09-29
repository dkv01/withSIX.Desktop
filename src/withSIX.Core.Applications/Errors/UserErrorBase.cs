// <copyright company="SIX Networks GmbH" file="UserErrorBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;

namespace withSIX.Core.Applications.Errors
{
    public class RestartAsAdministratorUserError : UserErrorModel
    {
        public static readonly RecoveryCommandModel Restart = new RecoveryCommandModel("Restart as Administrator",
            o => RecoveryOptionResultModel.RetryOperation);

        public RestartAsAdministratorUserError(Dictionary<string, object> contextInfo = null,
            Exception innerException = null)
            : base(
                "The action requires admin rights, retry while running as administrator?",
                "It seems you don't have permission",
                new[] {Restart, RecoveryCommandModel.Cancel}, contextInfo, innerException) {}
    }

    public class NotConnectedUserError : UserErrorModel
    {
        public NotConnectedUserError(Dictionary<string, object> contextInfo = null, Exception innerException = null)
            : base(
                "The action requires connection to withSIX, retry?", "It seems you've lost connection to withSIX",
                RecoveryCommands.RetryCommands, contextInfo, innerException) {}
    }

    public class NotLoggedInUserError : UserErrorModel
    {
        public NotLoggedInUserError(Dictionary<string, object> contextInfo = null, Exception innerException = null)
            : base(
                "The action requires login to withSIX, retry?", "It seems you're not logged in to withSIX",
                RecoveryCommands.RetryCommands, contextInfo, innerException) {}
    }

    public class BusyUserError : UserErrorModel
    {
        public BusyUserError(Dictionary<string, object> contextInfo = null, Exception innerException = null)
            : base(
                "The system is currently busy. Retry?",
                "You cannot perform the selected action until the system is no longer busy.",
                RecoveryCommands.RetryCommands, contextInfo, innerException) {}
    }

    public class CanceledUserError : UserErrorModel
    {
        public CanceledUserError(string errorMessage, string errorCauseOrResolution = null,
            IEnumerable<RecoveryCommandModel> recoveryOptions = null, Dictionary<string, object> contextInfo = null,
            OperationCanceledException innerException = null)
            : base(errorMessage, errorCauseOrResolution, recoveryOptions, contextInfo, innerException) {}
    }
}