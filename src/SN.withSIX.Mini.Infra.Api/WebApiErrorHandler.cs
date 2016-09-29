// <copyright company="SIX Networks GmbH" file="WebApiErrorHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using withSIX.Core;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Extensions;
using withSIX.Core.Infra.Services;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Usecases.Main;

namespace withSIX.Mini.Infra.Api
{
    public interface IWebApiErrorHandler
    {
        Task<RecoveryOptionResult> Handler(UserError error);
    }

    public class WebApiErrorHandler : ErrorHandler, IWebApiErrorHandler, IInfrastructureService
    {
        private readonly IStateHandler _api;

        public WebApiErrorHandler(IStateHandler api) {
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

        async Task<RecoveryOptionResult> HandleUserError(UserError error) {
            var t2 = error.RecoveryOptions.GetTask();
            await _api.AddUserError(new UserErrorModel2(error,
                error.RecoveryOptions.Select(
                    x =>
                        new RecoveryOptionModel {
                            CommandName = x.CommandName
                        }).ToList())).ConfigureAwait(false);
            //error.RecoveryOptions.First(x => x.CommandName == r).Execute(null);
            return await t2;
            //return error.RecoveryOptions.First(x => x.RecoveryResult.HasValue).RecoveryResult.Value;
        }

        private async Task<RecoveryOptionResult> ErrorDialog(UserError userError) {
            if (Common.Flags.IgnoreErrorDialogs)
                return RecoveryOptionResult.FailOperation;
            return await HandleUserError(userError).ConfigureAwait(false);
        }

        Task<RecoveryOptionResult> BasicMessageHandler(UserError userError) {
            MainLog.Logger.Error(userError.InnerException.Format());
            //var id = Guid.Empty;
#if !DEBUG
//var ex = new UserException(userError.ErrorMessage, userError.InnerException);
//id = ex.Id;
            Report(userError.InnerException);
#endif
            // NOTE: this code really shouldn't throw away the MessageBoxResult
            var message = userError.ErrorCauseOrResolution +
                          "\n\nWe've been notified about the problem." +
                          "\n\nPlease make sure you are running the latest version of the software.\n\nIf the problem persists, please contact Support: http://community.withsix.com";
            var title = userError.ErrorMessage ?? "An error has occured while trying to process the action";
            return HandleUserError(new UserError(title, message,
                new[] {new RecoveryCommandImmediate("OK", x => RecoveryOptionResult.CancelOperation)},
                userError.ContextInfo, userError.InnerException));
        }
    }
}