// <copyright company="SIX Networks GmbH" file="Program.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Core.Services;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases.Main;
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
            Common.Flags = new Common.StartupFlags(args, true);

            var bs = new CoreAppBootstrapper(args, rootPath);
            bs.Configure();

            TaskExt.StartLongRunningTask(() => bs.Startup(async () => { })).WaitAndUnwrapException();

            Console.WriteLine("Hello world!");
        }
    }

    public class StateHandlerDummy : IStateHandler, IPresentationService {
        public IObservable<ActionTabState> StatusObservable { get; }
        public IDictionary<Guid, GameStateHandler> Games { get; }
        public ActionTabState Current { get; }
        public UserErrorModel2[] UserErrors { get; }
        public ClientInfo ClientInfo { get; }
        public Guid SelectedGameId { get; set; }
        public Task Initialize() {
            return TaskExt.Default;
        }

        public Task<Unit> DispatchNextAction(Func<IAsyncVoidCommand, Task<Unit>> dispatcher, Guid requestId) {
            throw new NotImplementedException();
        }

        public Task ResolveError(Guid id, string result, Dictionary<string, object> data) {
            throw new NotImplementedException();
        }

        public Task AddUserError(UserErrorModel2 error) {
            throw new NotImplementedException();
        }

        public Task StartUpdating() {
            throw new NotImplementedException();
        }

        public Task UpdateAvailable(string version, AppUpdateState appUpdateState = AppUpdateState.UpdateAvailable) {
            throw new NotImplementedException();
        }
    }

    public class Bridge : IBridge, IPresentationService {
        // http://stackoverflow.com/questions/27080363/missingmethodexception-with-newtonsoft-json-when-using-typenameassemblyformat-wi

        public JsonSerializerSettings GameContextSettings() => new JsonSerializerSettings {
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
            // TODO: Very dangerous because we cant load/save when versions change?!? http://stackoverflow.com/questions/32245340/json-net-error-resolving-type-in-powershell-cmdlet
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.All,
            Error = OnError,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        }.SetDefaultConverters();

        private void OnError(object sender, ErrorEventArgs e) {
            //MainLog.Logger.Warn($"Error during JSON serialization for {e.CurrentObject}, {e.ErrorContext.Path} {e.ErrorContext.Member}: {e.ErrorContext.Error.Message}");
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