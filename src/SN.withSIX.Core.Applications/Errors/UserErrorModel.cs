using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SN.withSIX.Core.Applications.Errors
{
    public class UserErrorModel
    {
        public UserErrorModel(
            string errorMessage,
            string errorCauseOrResolution = null,
            IEnumerable<RecoveryCommandModel> recoveryOptions = null,
            Dictionary<string, object> contextInfo = null,
            Exception innerException = null) {
            ErrorMessage = errorMessage;
            ErrorCauseOrResolution = errorCauseOrResolution;
            RecoveryOptions = recoveryOptions ?? Enumerable.Empty<RecoveryCommandModel>();
            ContextInfo = contextInfo ?? new Dictionary<string, object>();
            ContextInfo["$$$Type"] = GetType().ToString();
            InnerException = innerException;
        }

        public Exception InnerException { get; set; }

        /// <summary>
        /// The "Newspaper Headline" of the message being conveyed to the
        /// user. This should be one line, short, and informative.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Additional optional information to describe what is happening, or
        /// the resolution to an information-only error (i.e. a dialog to tell
        /// the user that something has happened)
        /// </summary>
        public string ErrorCauseOrResolution { get; set; }
        public IEnumerable<RecoveryCommandModel> RecoveryOptions { get; set; }
        public Dictionary<string, object> ContextInfo { get; set; }

        /// <summary>
        /// This object is either a custom icon (usually an ImageSource), or
        /// it can also be a StockUserErrorIcon. It can also be an
        /// application-defined type that the handlers know to interpret.
        /// </summary>
        public object UserErrorIcon { get; set; }
    }

    public class RecoveryCommandModel
    {
        public RecoveryCommandModel(string commandName, Func<object, RecoveryOptionResultModel> handler = null) {
            CommandName = commandName;
            Handler = handler;
        }
        public bool IsDefault { get; set; }
        public bool IsCancel { get; set; }
        public string CommandName { get; protected set; }
        public RecoveryOptionResultModel? RecoveryResult { get; set; }
        public Func<object, RecoveryOptionResultModel> Handler = null;

        /// <summary>
        /// A default command whose caption is "Ok"
        /// </summary>
        /// <value>RetryOperation</value>
        public static RecoveryCommandModel Ok
        {
            get { var ret = new RecoveryCommandModel("Ok", _ => RecoveryOptionResultModel.RetryOperation) { IsDefault = true }; return ret; }
        }

        /// <summary>
        /// A default command whose caption is "Continue"
        /// </summary>
        /// <value>RetryOperation</value>
        public static RecoveryCommandModel Continue
        {
            get { var ret = new RecoveryCommandModel("Continue", _ => RecoveryOptionResultModel.RetryOperation) { IsDefault = true }; return ret; }
        }

        /// <summary>
        /// A default command whose caption is "Cancel"
        /// </summary>
        /// <value>FailOperation</value>
        public static RecoveryCommandModel Cancel
        {
            get { var ret = new RecoveryCommandModel("Cancel", _ => RecoveryOptionResultModel.CancelOperation) { IsCancel = true }; return ret; }
        }

        /// <summary>
        /// A default command whose caption is "Yes"
        /// </summary>
        /// <value>RetryOperation</value>
        public static RecoveryCommandModel Yes
        {
            get { var ret = new RecoveryCommandModel("Yes", _ => RecoveryOptionResultModel.RetryOperation) { IsDefault = true }; return ret; }
        }

        /// <summary>
        /// A default command whose caption is "No"
        /// </summary>
        /// <value>FailOperation</value>
        public static RecoveryCommandModel No
        {
            get { var ret = new RecoveryCommandModel("No", _ => RecoveryOptionResultModel.CancelOperation) { IsCancel = true }; return ret; }
        }
    }

    public enum RecoveryOptionResultModel
    {
        RetryOperation,
        CancelOperation,
        FailOperation
    }
}