// <copyright company="SIX Networks GmbH" file="WpfErrorHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using ReactiveUI;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Applications.MVVM.ViewModels.Dialogs;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation.Services;

namespace SN.withSIX.Core.Presentation.Wpf
{
    public class WpfErrorHandler : ErrorHandler
    {
        readonly IDialogManager _dialogManager;
        private readonly ISpecialDialogManager _specialDialogManager;

        public WpfErrorHandler(IDialogManager dialogManager, ISpecialDialogManager specialDialogManager) {
            _dialogManager = dialogManager;
            _specialDialogManager = specialDialogManager;
        }

        public Task<RecoveryOptionResult> Handler(UserError error, Window window = null) {
            if (error is CanceledUserError)
                return Task.FromResult(RecoveryOptionResult.CancelOperation);
            return error.RecoveryOptions != null && error.RecoveryOptions.Any()
                ? ErrorDialog(error, window)
                : BasicMessageHandler(error, window);
        }

        private async Task<RecoveryOptionResult> ErrorDialog(UserError error, Window window = null) {
            MainLog.Logger.FormattedWarnException(error.InnerException, "UserError");
            if (Common.Flags.IgnoreErrorDialogs)
                return RecoveryOptionResult.FailOperation;
            var settings = new Dictionary<string, object>();
            if (window != null)
                settings["Owner"] = window;
            var t2 = RecoveryCommandImmediate.GetTask(error.RecoveryOptions);
            await _specialDialogManager.ShowDialog(new UserErrorViewModel(error), settings).ConfigureAwait(false);
            return await t2.ConfigureAwait(false);
        }

        async Task<RecoveryOptionResult> BasicMessageHandler(UserError userError, Window window) {
            MainLog.Logger.Error(userError.InnerException.Format());
            //var id = Guid.Empty;

            Report(userError.InnerException);
            // NOTE: this code really shouldn't throw away the MessageBoxResult
            var message = userError.ErrorCauseOrResolution +
                          "\n\nWe've been notified about the problem." +
                          "\n\nPlease make sure you are running the latest version of the software.\n\nIf the problem persists, please contact Support: http://community.withsix.com";
            var title = userError.ErrorMessage ?? "An error has occured while trying to process the action";
            var result =
                await
                    _dialogManager.MessageBox(new MessageBoxDialogParams(message, title) {Owner = window})
                        .ConfigureAwait(false);
            return RecoveryOptionResult.CancelOperation;
        }

        /*
        async Task<RecoveryOptionResult> UnhandledError(UserError error, Window window = null) {
            var message = error.ErrorCauseOrResolution;
            var title = error.ErrorMessage;
            var result = await _dialogManager.ExceptionDialog(error.InnerException.UnwrapExceptionIfNeeded(),
                message, title, window).ConfigureAwait(false);
            //await _dialogManager.ExceptionDialogAsync(x.InnerException, x.ErrorCauseOrResolution,
            //x.ErrorMessage);
            //var result = await Dispatcher.InvokeAsync(() => _exceptionHandler.HandleException(arg.InnerException));
            // TODO: Should actually fail and rethrow as an exception to then be catched by unhandled exception handler?
            // TODO: Add proper retry options. e.g for Connect - dont just show a connect dialog, but make it part of the retry flow;
            // pressing Connect, and succesfully connect, should then retry the action?
            return result ? RecoveryOptionResult.CancelOperation : RecoveryOptionResult.FailOperation;
        }
        */
    }
}