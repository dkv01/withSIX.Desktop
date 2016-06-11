// <copyright company="SIX Networks GmbH" file="HubBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using ReactiveUI;
using ShortBus;
using SN.withSIX.Api.Models.Exceptions;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;

namespace SN.withSIX.Mini.Infra.Api.Hubs
{
    public abstract class HubBase<T> : Hub<T>, IUsecaseExecutor where T : class
    {
        protected Task<TResponseData> RequestAsync<TResponseData>(ICompositeCommand<TResponseData> command) =>
            ApiAction(() => UsecaseExecutorExtensions.RequestAsync(this, command), command);

        protected Task<TResponse> RequestAsync<TResponse>(IAsyncRequest<TResponse> command)
            => ApiAction(() => UsecaseExecutorExtensions.RequestAsync(this, command), command);

        protected Task<UnitType> DispatchNextAction(Guid requestId)
            => Cheat.Mediator.DispatchNextAction(RequestAsync, requestId);

        async Task<TResponse> ApiAction<TResponse>(Func<Task<TResponse>> action, object command) {
            //var isUserAction = command.GetType().GetAttribute<ApiUserActionAttribute>() != null;
            var isCommand = command is IWrite;

            if (isCommand)
                await new GlobalLocked().Raise().ConfigureAwait(false);
            try {
                return await ExecuteCommand(action, command).ConfigureAwait(false);
            } finally {
                if (isCommand)
                    await new GlobalUnlocked().Raise().ConfigureAwait(false);
            }
        }

        private async Task<TResponse> ExecuteCommand<TResponse>(Func<Task<TResponse>> action, object command) {
            retry:
            try {
                return await TryExecuteCommand(action).ConfigureAwait(false);
            } catch (AlreadyExistsException e) {
                // don't log
                throw new HubException(e.Message, e);
            } catch (ValidationException e) {
                // don't log
                throw new HubException(e.Message, e);
            } catch (UserException e) {
                MainLog.Logger.FormattedWarnException(e,
                    "UserException catched during hub action: " + command.GetType().Name);
                throw new HubException(e.Message, e);
            } // TODO: A better way to handle this actually from within frontends...
            catch (Exception ex) {
                var handleException = UiTaskHandler.HandleException(ex, "Action: " + command.GetType().Name);
                var result =
                    await UserError.Throw(handleException);
                if (result == RecoveryOptionResult.RetryOperation)
                    goto retry;
                throw new HubException("Operation aborted", new CanceledException("The operation was aborted", ex));
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

    public class GlobalLocked : IDomainEvent {}

    public class GlobalUnlocked : IDomainEvent {}
}