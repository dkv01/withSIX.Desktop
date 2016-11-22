// <copyright company="SIX Networks GmbH" file="SetupNlog.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Configuration;
using NLog;
using NLog.Config;
using NLog.Targets;
using withSIX.Core.Logging;

namespace withSIX.Core.Presentation.Bridge.Logging
{
    public class SetupNlog
    {
        public static void Initialize(string appName) {
            MainLog.logManager = new Lazy<ILogManager>(() => new DefaultLogManager());
            // TODO: This can cause StackOverFlow when having electron.config copied from sync.config
            var loggingConfiguration = LogManager.Configuration;
            if (loggingConfiguration == null)
                LogManager.Configuration = CreateDefaultConfig(appName);
            DefaultLogManager.Factory = new LogFactory(LogManager.Configuration);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        }


        public static void CurrentDomainOnUnhandledException(object sender,
            UnhandledExceptionEventArgs unhandledExceptionEventArgs) {
            var ex = unhandledExceptionEventArgs.ExceptionObject as Exception;
            if (ex == null)
                MainLog.Logger.Error(
                    "Catched unhandled exception in appdomain, but exception object is not of type Exception!");
            else
                MainLog.Logger.FormattedErrorException(ex, "Catched unhandled exception in appdomain");
        }

        static LoggingConfiguration CreateDefaultConfig(string appName) {
            var config = new LoggingConfiguration();
            var target = CreateDefaultFileTarget(appName);
            config.AddTarget("logfile", target);
            config.LoggingRules.Add(CreateDefaultLoggingRule(target, LogLevel.Trace));

            if (ConfigurationManager.AppSettings["Logentries.Token"] != null)
                SetupLogEntries(appName, config);
            return config;
        }

        private static void SetupLogEntries(string appName, LoggingConfiguration config) {
            var target2 = CreateLogEntriesTarget(appName);
            config.AddTarget("logentries", target2);
            config.LoggingRules.Add(CreateDefaultLoggingRule(target2, LogLevel.Error, true));
        }

        static LoggingRule CreateDefaultLoggingRule(Target target, LogLevel level = null, bool final = false)
            => new LoggingRule("withSIX.*", level ?? Default, target) {Final = final};

        private static readonly LogLevel Default =
#if DEBUG
            LogLevel.Trace;
#else
            LogLevel.Info;
#endif


        static Target CreateLogEntriesTarget(string appName) => new LogentriesTarget {
            Name = "logentries",
            Debug = true,
            HttpPut = false,
            Ssl = false,
            Layout =
                "${logger} : ${LEVEL} [${assembly-version}], ${message}"
        };

        static FileTarget CreateDefaultFileTarget(string appName) => new FileTarget {
            Name = "logfile",
            FileName =
                "${specialfolder:folder=LocalApplicationData}/SIX Networks/" +
                string.Format("{0}/logs/{0}.log", appName),
            ArchiveFileName =
                "${specialfolder:folder=LocalApplicationData}/SIX Networks/" +
                string.Format("{0}/logs/{0}", appName) + "_{#####}.log",
            ArchiveAboveSize = 1048576,
            ArchiveNumbering = ArchiveNumberingMode.Sequence,
            MaxArchiveFiles = 5,
            ConcurrentWrites = true,
            CreateDirs = true,
            Layout = "${date:format=ddd MMM dd} ${time:format=HH:mm:ss} ${date:format=zzz yyyy} ${logger} : ${LEVEL} [${assembly-version}], ${message}"
        };
    }
}