// <copyright company="SIX Networks GmbH" file="Api.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Applications.Usecases.Main.Games;
using SN.withSIX.Mini.Applications.Usecases.Settings;

namespace SN.withSIX.Mini.Presentation.Wpf.Node
{
    public class Api : IUsecaseExecutor
    {
        private readonly Excecutor _executor = new Excecutor();
        public Task<object> Invoke(dynamic input) {
            try {
                var request = (string) input.request;
                var requestData = input.data ?? new Dictionary<string, object>();

                switch (request) {
                case "getSettings":
                    return Request<GetGeneralSettings, GeneralSettings>(requestData);
                case "saveSettings":
                    return Request<SaveGeneralSettings, UnitType>(requestData);
                case "saveLogs":
                    return VoidCommand<SaveLogs>(requestData);
                case "enableDiagnostics":
                    return VoidCommand<StartInDiagnosticsMode>(requestData);
                case "installExplorerExtension":
                    return VoidCommand<InstallExtension>(requestData);
                case "uninstallExplorerExtension":
                    return VoidCommand<RemoveExtension>(requestData);
                case "handleParameters":
                    return
                        HandleSingleInstanceCall(
                            Tools.Serialization.Json.LoadJson<SICall>(SerializationExtension.ToJson(requestData)));
                case "installContent":
                    return VoidCommand<InstallContent>(requestData);
                case "installContents":
                    return VoidCommand<InstallContents>(requestData);
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
            } catch (Exception ex) {
                Console.WriteLine(ex);
                throw;
            }
        }

        class SICall
        {
            public List<string> pars { get; set; }
        }

        private Task<object> HandleSingleInstanceCall(SICall parameters)
            => new SIHandler().HandleSingleInstanceCall(parameters.pars);

        Task<object> VoidCommand<T>(dynamic requestData) where T : IAsyncRequest<UnitType>
            => Request<T, UnitType>(requestData);

        async Task<object> Request<T, T2>(dynamic requestData) where T : IAsyncRequest<T2> {
            var data = SerializationExtension.ToJson(requestData);
            T request = Tools.Serialization.Json.LoadJson<T>(data);
            //Console.WriteLine("Calling {0}, with data: {1}, as request: {2}. MEdiator: {3}", typeof(T), data, request, Cheat.Mediator);
            return await _executor.ApiAction(() => this.RequestAsync(request),request, CreateException).ConfigureAwait(false);
        }

        private Exception CreateException(string s, Exception exception) => new UnhandledUserException(s, exception);
    }
}