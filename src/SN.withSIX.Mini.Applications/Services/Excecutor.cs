// <copyright company="SIX Networks GmbH" file="Excecutor.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using withSIX.Api.Models.Exceptions;
using SN.withSIX.Core.Applications;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.Mini.Applications.Services
{
    public class Excecutor : IUsecaseExecutor
    {
        public Task<TResponse> ApiAction<TResponse>(Func<Task<TResponse>> action, object command,
            Func<string, Exception, Exception> createException) => ExecuteCommand(action, command, createException);

        public async Task<TResponse> ExecuteCommand<TResponse>(Func<Task<TResponse>> action, object command, Func<string, Exception, Exception> createException) {
            retry:
            try {
                return await TryExecuteCommand(action).ConfigureAwait(false);
            } catch (AlreadyExistsException e) {
                // don't log
                throw createException(e.Message, e);
            } catch (ValidationException e) {
                // don't log
                throw createException(e.Message, e);
            } catch (UserException e) {
                MainLog.Logger.FormattedWarnException(e,
                    "UserException catched during hub action: " + command.GetType().Name);
                throw createException(e.Message, e);
            } // TODO: A better way to handle this actually from within frontends...
            catch (Exception ex) {
                var handleException = ErrorHandlerr.HandleException(ex, "Action: " + command.GetType().Name);
                var result =
                    await UserErrorHandler.HandleUserError(handleException);
                if (result == RecoveryOptionResultModel.RetryOperation)
                    goto retry;
                throw createException("Operation aborted", new CanceledException("The operation was aborted", ex));
            }
        }

        private async Task<TResponse> TryExecuteCommand<TResponse>(Func<Task<TResponse>> action) {
            // TODO: Handle more global or deeper
            try {
                try {
                    var r = await action().ConfigureAwait(false);
                    return r;
                } catch (Win32Exception ex) {
                    if (ex.NativeErrorCode == 112)
                        throw GetException(ex.NativeErrorCode, ex);
                    if (ex.IsElevationCancelled())
                        throw ex.HandleUserCancelled();
                    throw;
                } catch (IOException ex) {
                    var error = Marshal.GetLastWin32Error();
                    if (error == 112)
                        throw GetException(error, ex);
                    throw;
                } catch (System.ComponentModel.DataAnnotations.ValidationException e) {
                    throw new ValidationException(e.Message, e);
                }
            } catch (OperationCanceledException ex) {
                MainLog.Logger.FormattedDebugException(ex, "The user cancelled the operation");
                throw new CanceledException(ex);
            }
        }

        private static NotEnoughFreeDiskSpaceException GetException(int error, Exception ex)
            => new NotEnoughFreeDiskSpaceException(WindowsAPIErrorCodes.ToString(error), ex);
    }
}