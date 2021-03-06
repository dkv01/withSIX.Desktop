﻿// <copyright company="SIX Networks GmbH" file="Api.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using ReactiveUI;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Services;
using withSIX.Mini.Applications;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Features;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Features.Main.Games;
using withSIX.Mini.Applications.Features.Settings;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Presentation.Core;

namespace withSIX.Mini.Presentation.Electron
{
    public class NodeApi : INodeApi
    {
        private readonly Func<object, Task<object>> _displayTrayBaloon;
        private readonly Func<object, Task<object>> _downloadFile;
        private readonly Func<object, Task<object>> _downloadSession;
        private readonly Func<object, Task<object>> _exit;
        private readonly Func<object, Task<object>> _handleUserError;
        private readonly Func<object, Task<object>> _installSelfUpdate;
        private readonly Func<object, Task<object>> _setState;
        private readonly Func<object, Task<object>> _showMessageBox;
        private readonly Func<object, Task<object>> _showNotification;
        private readonly Func<object, Task<object>> _showOpenDialog;
        private readonly Func<object, Task<object>> _showSaveDialog;

        public NodeApi(dynamic api) {
            _handleUserError = api.handleUserError as Func<object, Task<object>>;
            _showMessageBox = api.showMessageBox as Func<object, Task<object>>;
            _showOpenDialog = api.showOpenDialog as Func<object, Task<object>>;
            _showSaveDialog = api.showSaveDialog as Func<object, Task<object>>;
            _showNotification = api.showNotification as Func<object, Task<object>>;
            _displayTrayBaloon = api.displayTrayBaloon as Func<object, Task<object>>;
            _downloadFile = api.downloadFile as Func<object, Task<object>>;
            _downloadSession = api.downloadSession as Func<object, Task<object>>;
            _setState = api.setState as Func<object, Task<object>>;
            _installSelfUpdate = api.installSelfUpdate as Func<object, Task<object>>;
            _exit = api.exit as Func<object, Task<object>>;
            Version = api.version;
            if (api.args != null) {
                Args = new ArgsO {
                    /* Dynamic = api.args.multi, */
                    // TODO: 
                    Port = api.args.port,
                    WorkingDirectory = api.args.workingDirectory
                };
            }
        }

        public ArgsO Args { get; }

        public string Version { get; }

        public async Task<RecoveryOptionResult> HandleUserError(UserError error) {
            var t2 = error.RecoveryOptions.GetTask();
            Console.WriteLine("Handling error" + error);
            var r = (string) await _handleUserError(new UserErrorModel2(error,
                error.RecoveryOptions.Select(
                    x =>
                        new RecoveryOptionModel {
                            CommandName = x.CommandName
                        }).ToList())).ConfigureAwait(false);
            Console.WriteLine("got response" + r + r.GetType());
            var cmd = error.RecoveryOptions.First(x => x.CommandName == r);
            //cmd.RecoveryResult = RecoveryOptionResult.RetryOperation;
            Console.WriteLine("found command: " + cmd + cmd.RecoveryResult);
            cmd.Execute(null);
            Console.WriteLine("Executed, waiting" + cmd.RecoveryResult);
            return await t2;
            //return error.RecoveryOptions.First(x => x.RecoveryResult.HasValue).RecoveryResult.Value;
        }

        public async Task<IAbsoluteFilePath> DownloadFile(Uri url, string path, CancellationToken token) {
            var t = _downloadFile(new {url, path});
            using (var tc = token.ThrowWhenCanceled()) {
                await Task.WhenAny(t, tc.Task).ConfigureAwait(false);
                var r = await t.ConfigureAwait(false);
                return ((string) r).ToAbsoluteFilePath();
            }
        }

        public async Task DownloadSession(Uri url, string path, CancellationToken token) {
            var t = _downloadSession(new {url, path});
            using (var tc = token.ThrowWhenCanceled()) {
                await Task.WhenAny(t, tc.Task).ConfigureAwait(false);
                await t.ConfigureAwait(false);
            }
        }

        public async Task<string[]> ShowFileDialog(string title = null, string defaultPath = null) {
            var properties = new[] {"openFile"};
            var r = await _showOpenDialog(new {title, defaultPath, properties}).ConfigureAwait(false);
            var rO = (object[]) r;
            return rO?.Cast<string>().ToArray();
        }

        public async Task<bool?> ShowNotification(string title, string message = null)
            => (bool?) await _showNotification(new {title, message}).ConfigureAwait(false);

        public async Task<string> ShowSaveDialog(string title = null, string defaultPath = null) {
            var r = await _showSaveDialog(new {title, defaultPath}).ConfigureAwait(false);
            return (string) r;
        }

        public async Task<string[]> ShowFolderDialog(string title = null, string defaultPath = null) {
            var properties = new[] {"openDirectory"};
            var r = await _showOpenDialog(new {title, defaultPath, properties}).ConfigureAwait(false);
            var rO = (object[]) r;
            return rO?.Cast<string>().ToArray();
        }

        public Task DisplayTrayBaloon(string title, string content, string icon = null)
            => _displayTrayBaloon(new {title, content, icon});

        public Task SetState(BusyState state, string description, double? progress)
            => _setState(new StateInfo {state = (int) state, description = description, progress = progress});

        public Task InstallSelfUpdate() => _installSelfUpdate(true);

        public Task Exit(int exitCode) => _exit(exitCode);

        // TODO: Icon and type etc..
        public async Task<string> ShowMessageBox(string title, string message, string[] buttons, string type = null) {
            var r = await _showMessageBox(new {title, message, buttons, type}).ConfigureAwait(false);
            return (string) r;
        }

        class StateInfo
        {
            public int state { get; set; }
            public string description { get; set; }
            public double? progress { get; set; }
        }
    }

    public class Api : IUsecaseExecutor
    {
        private readonly Excecutor _executor = new Excecutor();

        public async Task<object> Invoke(dynamic input) {
            try {
                return await TryInvoke(input).ConfigureAwait(false);
            } catch (Exception ex) {
                Console.WriteLine(ex);
                MainLog.Logger.FormattedErrorException(ex);
                throw;
            }
        }

        private Task<object> TryInvoke(dynamic input) {
            var request = (string) input.request;
            var requestData = (object) input.data ?? new Dictionary<string, object>();

            switch (request) {
            case "getSettings":
                return Request<GetGeneralSettings, GeneralSettings>(requestData);
            case "saveSettings":
                return VoidCommand<SaveGeneralSettings>(requestData);
            case "saveLogs":
                return VoidCommand<SaveLogs>(requestData);
            case "enableDiagnostics":
                return VoidCommand<StartInDiagnosticsMode>(requestData);
            case "installExplorerExtension":
                return VoidCommand<InstallExtension>(requestData);
            case "uninstallExplorerExtension":
                return VoidCommand<RemoveExtension>(requestData);
            case "handleParameters":
                return HandleSingleInstanceCall(Unpack<SICall>(requestData));
            case "installContent":
                return VoidCommand<InstallContent>(requestData);
            case "installContents":
                return VoidCommand<InstallContents>(requestData);
            case "installSteamContents":
                return VoidCommand<InstallSteamContents>(requestData);
            case "uninstallContent":
                return VoidCommand<UninstallContent>(requestData);
            case "uninstallContents":
                return VoidCommand<UninstallContents>(requestData);
            case "launchContent":
                return VoidCommand<LaunchContent>(requestData);
            case "launchContents":
                return VoidCommand<LaunchContents>(requestData);
            case "closeGame":
                return VoidCommand<CloseGame>(requestData);
            default:
                throw new Exception("Unknown command");
            }
        }

        private Task<object> HandleSingleInstanceCall(SICall parameters)
            => new SIHandler().HandleSingleInstanceCall(parameters.pars);

        Task<object> VoidCommand<T>(object requestData) where T : ICommand
            => Request<T>(requestData).VoidObject();

        async Task<object> Request<T, T2>(object requestData) where T : IRequest<T2> {
            var request = Unpack<T>(requestData);
            //Console.WriteLine("Calling {0}, with data: {1}, as request: {2}. MEdiator: {3}", typeof(T), data, request, Cheat.Mediator);
            return
                await _executor.ApiAction(() => this.Send(request), request, CreateException).ConfigureAwait(false);
        }

        async Task Request<T>(object requestData) where T : IRequest
        {
            var request = Unpack<T>(requestData);
            //Console.WriteLine("Calling {0}, with data: {1}, as request: {2}. MEdiator: {3}", typeof(T), data, request, Cheat.Mediator);
            await _executor.ApiAction(() => this.Send(request), request, CreateException).ConfigureAwait(false);
        }

        private static T Unpack<T>(object requestData) {
            var data = requestData.ToJson();
            return data.FromJson<T>();
        }

        private Exception CreateException(string s, Exception exception) => new UnhandledUserException(s, exception);

        class SICall
        {
            public List<string> pars { get; set; }
        }
    }
}