// <copyright company="SIX Networks GmbH" file="Logger.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using NLog;
using SN.withSIX.Core.Extensions;

namespace SN.withSIX.Core.Logging
{
    public class LoggerBase : ILogger
    {
        readonly Logger _logger;

        public LoggerBase(Logger logger) {
            _logger = logger;
        }

        public LoggerBase(string type)
            : this(DefaultLogManager.Factory.GetLogger(type)) {}

        public void Info(string message) => _logger.Info(message);

        public void Debug(string message) {
            System.Diagnostics.Debug.WriteLine(message);
            _logger.Debug(message);
        }

        public void Trace(string message) {
            System.Diagnostics.Trace.WriteLine(message);
            _logger.Trace(message);
        }

        public void Warn(string message) => _logger.Warn(message);

        public void Error(string message) => _logger.Error(message);

        public void Vital(string message) => _logger.Info(message);

        public void Info(string message, params object[] args) => _logger.Info(message, args);

        public void Debug(string message, params object[] args) {
            System.Diagnostics.Debug.WriteLine(message, args);
            _logger.Debug(message, args);
        }

        public void Trace(string message, params object[] args) {
            System.Diagnostics.Trace.WriteLine(message.FormatWith(args));
            _logger.Trace(message, args);
        }

        public void Warn(string message, params object[] args) => _logger.Warn(message, args);

        public void Error(string message, params object[] args) => _logger.Error(message, args);

        public void Vital(string message, params object[] args) => _logger.Info(message, args);

        public void FormattedDebugException(Exception e, string message = null)
            => _logger.Debug(e, FormatMessage(e, message));

        public void FormattedWarnException(Exception e, string message = null)
            => _logger.Warn(e, FormatMessage(e, message));

        public void FormattedErrorException(Exception e, string message = null)
            => _logger.Error(e, FormatMessage(e, message));

        public void FormattedFatalException(Exception e, string message = null)
            => _logger.Fatal(e, FormatMessage(e, message));

        public void Fatal(string message) => _logger.Fatal(message);
        public void Fatal(string message, params object[] args) => _logger.Fatal(message, args);

        static string FormatMessage(Exception e, string message) {
            if (string.IsNullOrWhiteSpace(message))
                return e.Format();
            return message + ": " + e.Format();
        }
    }
}