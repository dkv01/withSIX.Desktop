// <copyright company="SIX Networks GmbH" file="ILogger.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;

namespace withSIX.Core.Logging
{
    public interface ILogger
    {
        void Info(string message);
        void Debug(string message);
        void Trace(string message);
        void Warn(string message);
        void Error(string message);
        void Fatal(string message);
        void Info(string message, params object[] args);
        void Debug(string message, params object[] args);
        void Trace(string message, params object[] args);
        void Warn(string message, params object[] args);
        void Error(string message, params object[] args);
        void Fatal(string message, params object[] args);
    }


    public static class LoggerExtensions
    {
        public static void FormattedDebugException(this ILogger _logger, Exception e, string message = null)
            => _logger.Debug(FormatMessage(e, message));

        public static void FormattedWarnException(this ILogger _logger, Exception e, string message = null)
            => _logger.Warn(FormatMessage(e, message));

        public static void FormattedErrorException(this ILogger _logger, Exception e, string message = null)
            => _logger.Error(FormatMessage(e, message));

        public static void FormattedFatalException(this ILogger _logger, Exception e, string message = null)
            => _logger.Fatal(FormatMessage(e, message));

        static string FormatMessage(Exception e, string message) {
            if (string.IsNullOrWhiteSpace(message))
                return e.Format();
            return message + ": " + e.Format();
        }
    }
}