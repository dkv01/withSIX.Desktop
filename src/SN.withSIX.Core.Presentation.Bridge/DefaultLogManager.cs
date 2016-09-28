// <copyright company="SIX Networks GmbH" file="DefaultLogManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NLog;
using SN.withSIX.Core.Logging;
using ILogger = SN.withSIX.Core.Logging.ILogger;

namespace SN.withSIX.Core.Presentation.Bridge
{
    public class DefaultLogManager : ILogManager
    {
        public static LogFactory Factory { get; set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual ILogger GetLogger(string type) => new LoggerBase(type);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual ILogger GetCurrentClassLogger() {
            var frame = new StackFrame(1, false);
            return new LoggerBase(LogManager.GetLogger(frame.GetMethod().DeclaringType.FullName));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual ILogger GetCurrentClassLogger(Type loggerType) {
            var frame = new StackFrame(1, false);
            return new LoggerBase(Factory.GetLogger(frame.GetMethod().DeclaringType.FullName, loggerType));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual ILogger GetCurrentClassLoggerOrMerged() {
            var frame = new StackFrame(1, false);
            return
                new LoggerBase(
                    Factory.GetLogger(frame.GetMethod().DeclaringType.FullName));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual ILogger GetCurrentClassLoggerOrMerged(Type loggerType) {
            var frame = new StackFrame(1, false);
            return
                new LoggerBase(Factory.GetLogger(frame.GetMethod().DeclaringType.FullName, loggerType));
        }

        public virtual ILogger GetLoggerOrMerged(Type type)
            => new LoggerBase(Factory.GetLogger(type.FullName));

        public virtual ILogger GetLoggerOrMerged(Type type, Type loggerType)
            => new LoggerBase(Factory.GetLogger(type.FullName, loggerType));
    }
}