// <copyright company="SIX Networks GmbH" file="LoggerBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using NLog;
using SN.withSIX.Core.Extensions;
using ILogger = SN.withSIX.Core.Logging.ILogger;

namespace SN.withSIX.Core.Presentation.Bridge
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

        public void Fatal(string message) => _logger.Fatal(message);
        public void Fatal(string message, params object[] args) => _logger.Fatal(message, args);
    }
}