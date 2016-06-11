// <copyright company="SIX Networks GmbH" file="ExceptionExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Reflection;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.Core.Extensions
{
    public static class ExceptionExtensions
    {
        public static Func<Exception, int, string> FormatException = (ex, level) => ex.ToString();
        public static Func<Exception, string, Exception> HandledException = (ex, message) => new Exception(message, ex);

        public static Exception UnwrapExceptionIfNeeded(this Exception ex)
            => ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;

        public static bool IsElevationCancelled(this Win32Exception ex)
            => ex.NativeErrorCode == Win32ErrorCodes.ERROR_CANCELLED_ELEVATION;

        public static bool IsOutOfDiskspace(this Win32Exception ex)
            => ex.NativeErrorCode == Win32ErrorCodes.ERROR_OUT_OF_DISKSPACE;

        public static OperationCanceledException HandleUserCancelled(this Win32Exception ex) {
            MainLog.Logger.FormattedWarnException(ex, "User canceled elevation action");
            return new OperationCanceledException("User canceled elevation action", ex);
        }

        public static string Format(this Exception e, int level = 0) {
            Contract.Requires<ArgumentNullException>(e != null);

            return FormatException(e, level);
        }
    }
}