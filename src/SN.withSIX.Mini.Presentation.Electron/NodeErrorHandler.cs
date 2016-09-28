// <copyright company="SIX Networks GmbH" file="NodeErrorHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Presentation.Core;

namespace SN.withSIX.Mini.Presentation.Electron
{
    public class NodeErrorHandler : ErrorHandler, IStdErrorHandler
    {
        private readonly INodeApi _api;

        public NodeErrorHandler(INodeApi api) {
            _api = api;
        }

        public Task<RecoveryOptionResult> Handler(UserError error) {
            if (error is CanceledUserError)
                return Task.FromResult(RecoveryOptionResult.CancelOperation);
            return (error.RecoveryOptions != null) && error.RecoveryOptions.Any()
                ? ErrorDialog(error)
                : /*#if DEBUG
                                UnhandledError(error);
                #else
                            BasicMessageHandler(error);
                #endif*/
                BasicMessageHandler(error);
        }

        private async Task<RecoveryOptionResult> ErrorDialog(UserError userError) {
            if (Common.Flags.IgnoreErrorDialogs)
                return RecoveryOptionResult.FailOperation;
            return await _api.HandleUserError(userError).ConfigureAwait(false);
        }

        Task<RecoveryOptionResult> BasicMessageHandler(UserError userError) {
            MainLog.Logger.Error(userError.InnerException.Format());
            //var id = Guid.Empty;
            Report(userError.InnerException);
            // NOTE: this code really shouldn't throw away the MessageBoxResult
            var message = userError.ErrorCauseOrResolution +
                          "\n\nWe've been notified about the problem." +
                          "\n\nPlease make sure you are running the latest version of the software.\n\nIf the problem persists, please contact Support: http://community.withsix.com";
            var title = userError.ErrorMessage ?? "An error has occured while trying to process the action";
            return
                _api.HandleUserError(new UserError(title, message,
                    new[] {new RecoveryCommandImmediate("OK", x => RecoveryOptionResult.CancelOperation)},
                    userError.ContextInfo, userError.InnerException));
        }
    }
}