// <copyright company="SIX Networks GmbH" file="DefaultWpfExceptionhandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Akavache.Sqlite3.Internal;
using withSIX.Core.Applications.Errors;
using withSIX.Sync.Core.Repositories;
using withSIX.Sync.Core.Repositories.Internals;

namespace withSIX.Core.Presentation.Services
{
    public abstract class DefaultExceptionHandler : IExceptionHandler
    {
        private readonly ICollection<IHandleExceptionPlugin> _handlers;

        protected DefaultExceptionHandler(IEnumerable<IHandleExceptionPlugin> ehs) {
            _handlers = ehs.ToList();
        }

        public UserErrorModel HandleException(Exception ex, string action = "Action") {
            Contract.Requires<ArgumentNullException>(action != null);

            var unwrapped = UnwrapExceptionIfNeeded(ex);

            foreach (var r in _handlers.Select(h => h.HandleException(unwrapped, action)).Where(r => r != null))
                return r;
            return HandleExceptionInternal((dynamic) unwrapped, action);
        }

        public async Task<bool> TryExecuteAction(Func<Task> action, string message = null) {
            Exception e = null;
            try {
                await action().ConfigureAwait(false);
                return true;
            } catch (Exception ex) {
                e = ex;
            }
            return await UserErrorHandler.HandleUserError(HandleException(e)) != RecoveryOptionResultModel.FailOperation;
            //return false;
        }

        protected virtual UserErrorModel HandleExceptionInternal(Exception ex, string action = "Action")
            => Handle((dynamic) ex, action);

        protected static UserErrorModel Handle(RepositoryLockException ex, string action) =>
            new BasicUserError("It seems another program is locking the repository",
                "Please close other applications, like Play withSIX, and try again", innerException: ex);

        protected static UserErrorModel Handle(ChecksumException ex, string action)
            => HandleCorruptionException(ex, action);

        protected static UserErrorModel Handle(CompressedFileException ex, string action)
            => HandleCorruptionException(ex, action);

        private static UserErrorModel HandleCorruptionException(Exception ex, string action)
            => new BasicUserError("An error has occured while trying to '" + action + "'",
                @"Files appear to be corrupted.

1. Make sure no firewall/proxy is blocking or replacing requests to our network (exclude our program and tools (zsync/rsync))
2. Make sure you have enough free disk space
3. Try Diagnosing the mods in question (Instead of Install/Update, choose the Diagnose action)

More info: " + ex.Message, innerException: ex);

        protected static UserErrorModel Handle(OperationCanceledException ex, string action)
            => new CanceledUserError(action, innerException: ex);


        protected static UserErrorModel Handle(UnauthorizedAccessException ex, string action)
            =>
            new RecoverableUserError(ex, "Access denied",
                "Please make sure the path is writable and not in use by a running game or otherwise\n\nError info: " +
                ex.Message);

        protected static UserErrorModel Handle(HttpRequestException ex)
            =>
            new RecoverableUserError(ex, "Could not connect",
                "A http request has failed, is your internet connected? Are the DNS servers configured correctly?\n\nError info: " +
                ex.Message);

        //        protected static UserError Handle(Win32Exception ex, string action) {
        //            return ex.NativeErrorCode == Win32ErrorCodes.ERROR_CANCELLED_ELEVATION
        //                ? new CanceledUserError(action, innerException: ex)
        //                : Handle((Exception) ex, action);
        //        }

        protected static UserErrorModel Handle(SQLiteException ex, string action) {
            var message =
                $"There appears to be a problem with your database. If the problem persists, you can delete the databases from:\n{Common.Paths.LocalDataPath} and {Common.Paths.DataPath}" +
                "\nError message: " + ex.Message;
            var title = "An error has occured while trying to '" + action + "'";
            return new UserErrorModel(title, message, innerException: ex);
        }

        protected static UserErrorModel Handle(Exception ex, string action) {
            var message = "An unexpected error has occurred while trying to execute the requested action:" +
                          "\n" + ex.Message;
            var title = "An error has occured while trying to '" + action + "'";
            return new UserErrorModel(title, message, innerException: ex);
        }

        protected static string GetHumanReadableActionName(string action) => action.Split('.').Last();

        // TODO: ability to unwrap other exception kinds? -> Extension Method?
        protected static Exception UnwrapExceptionIfNeeded(Exception ex)
            => ex is TargetInvocationException && (ex.InnerException != null) ? ex.InnerException : ex;
    }
}