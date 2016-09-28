// <copyright company="SIX Networks GmbH" file="InformationalUserError.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.Core.Applications.Errors
{
    public class NonRecoveryCommand : RecoveryCommandModel, IDontRecover
    {
        public NonRecoveryCommand(string commandName) : base(commandName) {}
    }

    public static class UserErrorHandler
    {
        public static Func<UserErrorModel, Task<RecoveryOptionResultModel>> HandleUserError { get; set; }

        public static Task<RecoveryOptionResultModel> GeneralUserError(Exception exception, string title,
                string message)
            => HandleUserError(new UserErrorModel(title, message, new[] {RecoveryCommandModel.Ok}, null, exception));

        public static Task<RecoveryOptionResultModel> InformationalUserError(Exception exception, string title,
            string message) => HandleUserError(new InformationalUserError(exception, title, message));

        public static Task<RecoveryOptionResultModel> RecoverableUserError(Exception exception, string title,
            string message) => HandleUserError(new RecoverableUserError(exception, title, message));
    }

    public static class RecoveryCommands
    {
        public static readonly RecoveryCommandModel Retry = new RecoveryCommandModel("Retry",
            o => RecoveryOptionResultModel.RetryOperation);
        public static RecoveryCommandModel[] YesNoCommands = {RecoveryCommandModel.Yes, RecoveryCommandModel.No};
        public static RecoveryCommandModel[] RetryCommands = {Retry, RecoveryCommandModel.Cancel};
    }


    public interface IDontRecover {}

    public class BasicUserError : UserErrorModel
    {
        public BasicUserError(string errorMessage, string errorCauseOrResolution = null,
            Dictionary<string, object> contextInfo = null,
            Exception innerException = null)
            : base(
                errorMessage, errorCauseOrResolution, new[] {RecoveryCommandModel.Cancel}, contextInfo, innerException) {}
    }

    public class RecoverableUserError : UserErrorModel
    {
        public RecoverableUserError(Exception innerException, string errorMessage, string errorCauseOrResolution = null,
            Dictionary<string, object> contextInfo = null)
            : base(errorMessage, errorCauseOrResolution, RecoveryCommands.RetryCommands, contextInfo, innerException) {}
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
            : base(innerException, errorMessage, errorCauseOrResolution, contextInfo) {}
    }
}