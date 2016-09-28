// <copyright company="SIX Networks GmbH" file="Program.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reflection;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services;
using withSIX.Api.Models.Extensions;

namespace withSIX.Mini.Presentation.CoreHost
{
    public class Program
    {
        public static void Main(string[] args) {
            var entryAssembly = typeof(Program).GetTypeInfo().Assembly;
            MainLog.logManager = new Lazy<ILogManager>(() => new DummyLogManager());
            var rootPath = entryAssembly.Location.ToAbsoluteFilePath().ParentDirectoryPath;
            CommonBase.AssemblyLoader = new AssemblyLoader(entryAssembly, null, rootPath);

            var bs = new CoreAppBootstrapper(args, rootPath);
            bs.Configure();

            TaskExt.StartLongRunningTask(() => bs.Startup(async () => { })).WaitAndUnwrapException();

            Console.WriteLine("Hello world!");
        }
    }

    public class DummyLogger : ILogger {
        public void Info(string message) => Console.WriteLine($"Info: {message}");

        public void Debug(string message) => Console.WriteLine($"Debug: {message}");

        public void Trace(string message) => Console.WriteLine($"Trace: {message}");

        public void Warn(string message) => Console.WriteLine($"Warn: {message}");

        public void Error(string message) => Console.WriteLine($"Error: {message}");

        public void Fatal(string message) => Console.WriteLine($"Fatal: {message}");
        public void Info(string message, params object[] args) => Console.WriteLine($"Info: {string.Format(message, args)}");

        public void Debug(string message, params object[] args) => Console.WriteLine($"Debug: {string.Format(message, args)}");

        public void Trace(string message, params object[] args) => Console.WriteLine($"Trace: {string.Format(message, args)}");

        public void Warn(string message, params object[] args) => Console.WriteLine($"Warn: {string.Format(message, args)}");

        public void Error(string message, params object[] args) => Console.WriteLine($"Error: {string.Format(message, args)}");

        public void Fatal(string message, params object[] args) => Console.WriteLine($"Fatal: {string.Format(message, args)}");
    }

    public class DummyLogManager : ILogManager {
        public ILogger GetLogger(string type) => new DummyLogger();

        public ILogger GetCurrentClassLogger() => new DummyLogger();

        public ILogger GetCurrentClassLogger(Type loggerType) => new DummyLogger();

        public ILogger GetCurrentClassLoggerOrMerged() => new DummyLogger();
        public ILogger GetCurrentClassLoggerOrMerged(Type loggerType) => new DummyLogger();

        public ILogger GetLoggerOrMerged(Type type) => new DummyLogger();
    }
}