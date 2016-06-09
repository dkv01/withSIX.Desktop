// <copyright company="SIX Networks GmbH" file="Startup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation.Services;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases.Main;

namespace SN.withSIX.Mini.Presentation.Wpf.Node
{
    public class UpdateAvailable : IUsecaseExecutor
    {
        public async Task<object> Invoke(dynamic input) {
            var state = (UpdateState) (int) input.state;
            var version = input.version as string;
            await
                this.RequestAsync(new Applications.Usecases.Main.UpdateAvailable(state, version))
                    .ConfigureAwait(false);
            return true;
        }
    }

    public class StartupIsReady
    {
        public async Task<object> Invoke(dynamic input) => Cheat.Initialized;
    }

    public class Startup
    {
        public async Task<object> Invoke(dynamic input) {
            try {
                // doing this will stop the return of the promise...
                //await Task.Run(() => ByOther());
                var api = new NodeApi(input.api);

                ByOther(api);
                return true;
            } catch (Exception ex) {
                Console.WriteLine(ex.Format());
                throw;
            }
        }

        public void ByDomain(string path) {
            var dom = AppDomain.CreateDomain("MyDomain", null, path, string.Empty, false);
            dom.ExecuteAssemblyByName("Sync");
        }

        public void ByOther(NodeApi api) => Entrypoint.MainForNode(api);

        public class NodeErrorHandler : ErrorHandler, IStdErrorHandler
        {
            private readonly INodeApi _api;

            public NodeErrorHandler(INodeApi api) {
                _api = api;
            }

            public Task<RecoveryOptionResult> Handler(UserError error) {
                if (error is CanceledUserError)
                    return Task.FromResult(RecoveryOptionResult.CancelOperation);
                return error.RecoveryOptions != null && error.RecoveryOptions.Any()
                    ? ErrorDialog(error)
                    : /*#if DEBUG
                                UnhandledError(error);
                #else
                            BasicMessageHandler(error);
                #endif*/
                    BasicMessageHandler(error);
            }

            private async Task<RecoveryOptionResult> ErrorDialog(UserError userError) {
                if (Common.Flags.IgnoreErrorDialogs)
                    return RecoveryOptionResult.FailOperation;
                return await _api.HandleUserError(userError).ConfigureAwait(false);
            }

            Task<RecoveryOptionResult> BasicMessageHandler(UserError userError) {
                MainLog.Logger.Error(userError.InnerException.Format());
                //var id = Guid.Empty;
                Report(userError.InnerException);
                // NOTE: this code really shouldn't throw away the MessageBoxResult
                var message = userError.ErrorCauseOrResolution +
                              "\n\nWe've been notified about the problem." +
                              "\n\nPlease make sure you are running the latest version of the software.\n\nIf the problem persists, please contact Support: http://community.withsix.com";
                var title = userError.ErrorMessage ?? "An error has occured while trying to process the action";
                return
                    _api.HandleUserError(new UserError(title, message,
                        new[] {new RecoveryCommandImmediate("OK", x => RecoveryOptionResult.CancelOperation)},
                        userError.ContextInfo, userError.InnerException));
            }
        }

        public class NullNodeApi : INodeApi
        {
            public string Version { get; }
            public ArgsO Args { get; }

            public Task<RecoveryOptionResult> HandleUserError(UserError error) {
                throw new NotImplementedException();
            }

            public Task<string> ShowMessageBox(string title, string message, string[] buttons, string type = null) {
                throw new NotImplementedException();
            }

            public Task<string> ShowSaveDialog(string title = null, string defaultPath = null) {
                throw new NotImplementedException();
            }

            public Task<string[]> ShowFileDialog(string title = null, string defaultPath = null) {
                throw new NotImplementedException();
            }

            public Task<string[]> ShowFolderDialog(string title = null, string defaultPath = null) {
                throw new NotImplementedException();
            }

            public Task DisplayTrayBaloon(string title, string content, string icon = null) {
                throw new NotImplementedException();
            }

            public Task SetState(BusyState state, string description, double? progress) {
                throw new NotImplementedException();
            }

            public Task InstallSelfUpdate() {
                throw new NotImplementedException();
            }

            public Task Exit(int exitCode) {
                throw new NotImplementedException();
            }
        }

        public class NodeApi : INodeApi
        {
            private readonly Func<object, Task<object>> _displayTrayBaloon;
            private readonly Func<object, Task<object>> _exit;
            private readonly Func<object, Task<object>> _handleUserError;
            private readonly Func<object, Task<object>> _installSelfUpdate;
            private readonly Func<object, Task<object>> _setState;
            private readonly Func<object, Task<object>> _showMessageBox;
            private readonly Func<object, Task<object>> _showOpenDialog;
            private readonly Func<object, Task<object>> _showSaveDialog;

            public ArgsO Args { get; }

            public NodeApi(dynamic api) {
                _handleUserError = api.handleUserError as Func<object, Task<object>>;
                _showMessageBox = api.showMessageBox as Func<object, Task<object>>;
                _showOpenDialog = api.showOpenDialog as Func<object, Task<object>>;
                _showSaveDialog = api.showSaveDialog as Func<object, Task<object>>;
                _displayTrayBaloon = api.displayTrayBaloon as Func<object, Task<object>>;
                _setState = api.setState as Func<object, Task<object>>;
                _installSelfUpdate = api.installSelfUpdate as Func<object, Task<object>>;
                _exit = api.exit as Func<object, Task<object>>;
                Version = api.version;
                if (api.args != null)
                    Args = new ArgsO { /* Dynamic = api.args.multi, */ Port = api.args.port, WorkingDirectory = api.args.workingDirectory };
            }

            public string Version { get; }

            public async Task<RecoveryOptionResult> HandleUserError(UserError error) {
                var t2 = RecoveryCommandImmediate.GetTask(error.RecoveryOptions);
                Console.WriteLine("Handling error" + error);
                var r = (string) await _handleUserError(new UserErrorModel(error)).ConfigureAwait(false);
                Console.WriteLine("got response" + r + r.GetType());
                var cmd = error.RecoveryOptions.First(x => x.CommandName == r);
                //cmd.RecoveryResult = RecoveryOptionResult.RetryOperation;
                Console.WriteLine("found command: " + cmd + cmd.RecoveryResult);
                cmd.Execute(null);
                Console.WriteLine("Executed, waiting" + cmd.RecoveryResult);
                return await t2;
                //return error.RecoveryOptions.First(x => x.RecoveryResult.HasValue).RecoveryResult.Value;
            }

            public async Task<string[]> ShowFileDialog(string title = null, string defaultPath = null) {
                var properties = new[] {"openFile"};
                var r = await _showOpenDialog(new {title, defaultPath, properties}).ConfigureAwait(false);
                var rO = (object[]) r;
                return rO?.Cast<string>().ToArray();
            }

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
    }
}