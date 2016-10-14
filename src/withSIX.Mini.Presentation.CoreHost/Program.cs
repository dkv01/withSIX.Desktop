﻿// <copyright company="SIX Networks GmbH" file="Program.cs">
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
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Core.Logging;
using withSIX.Core.Presentation;
using withSIX.Core.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Usecases.Main;

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

            TaskExt.StartLongRunningTask(() => Start(bs)).WaitAndUnwrapException();

            Console.WriteLine("Hello world!");
            Console.ReadLine();
        }

        private static async Task Start(CoreAppBootstrapper bs) {
            await bs.Startup(async () => { }).ConfigureAwait(false);
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
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Binder = new NamespaceMigrationSerializationBinder()
        }.SetDefaultConverters();

        JsonSerializerSettings IBridge.OtherSettings() {
            return OtherSettings();
        }

        // TODO: Consider implications for the actual S-IR system messages: http://stackoverflow.com/questions/37832165/signalr-net-core-camelcase-json-contract-resolver/39410434#39410434
        public static JsonSerializerSettings OtherSettings() => new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            //TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
            // TODO: Very dangerous because we cant load/save when versions change?!? http://stackoverflow.com/questions/32245340/json-net-error-resolving-type-in-powershell-cmdlet
            //PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.Auto,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            //Binder = new NamespaceMigrationSerializationBinder(),
            Error = OnError,
        }.SetDefaultConverters();

        public class NamespaceMigrationSerializationBinder : DefaultSerializationBinder
        {
            //private readonly INamespaceMigration[] _migrations;

            public NamespaceMigrationSerializationBinder(/*params INamespaceMigration[] migrations*/) {
                //_migrations = migrations;
            }

            static readonly string searchStr = "SN.withSIX.";

            public override Type BindToType(string assemblyName, string typeName) {
                //var migration = _migrations.SingleOrDefault(p => p.FromAssembly == assemblyName && p.FromType == typeName);
                //if (migration != null) {
                //return migration.ToType;
                //}
                if (assemblyName.StartsWith(searchStr))
                    assemblyName = assemblyName.Substring(3);
                if (typeName.StartsWith(searchStr))
                    typeName = typeName.Substring(3);
                return base.BindToType(assemblyName, typeName);
            }
        }

        private static void OnError(object sender, ErrorEventArgs e) {
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