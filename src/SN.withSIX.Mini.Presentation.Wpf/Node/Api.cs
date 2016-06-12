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
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Applications.Usecases.Settings;

namespace SN.withSIX.Mini.Presentation.Wpf.Node
{
    public class Api : IUsecaseExecutor
    {
        private Excecutor _executor = new Excecutor();
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
                    return Request<SaveLogs, UnitType>(requestData);
                case "enableDiagnostics":
                    return Request<StartInDiagnosticsMode, UnitType>(requestData);
                case "installExplorerExtension":
                    return Request<InstallExtension, UnitType>(requestData);
                case "uninstallExplorerExtension":
                    return Request<RemoveExtension, UnitType>(requestData);
                case "handleParameters":
                    return HandleSingleInstanceCall(Tools.Serialization.Json.LoadJson<SICall>(SerializationExtension.ToJson(requestData)));
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

        async Task<object> Request<T, T2>(dynamic requestData) where T : IAsyncRequest<T2> {
            var data = SerializationExtension.ToJson(requestData);
            T request = Tools.Serialization.Json.LoadJson<T>(data);
            //Console.WriteLine("Calling {0}, with data: {1}, as request: {2}. MEdiator: {3}", typeof(T), data, request, Cheat.Mediator);
            return await _executor.ApiAction(() => this.RequestAsync(request),request, CreateException).ConfigureAwait(false);
        }

        private Exception CreateException(string s, Exception exception) => new UnhandledUserException(s, exception);
    }

    internal class UnhandledUserException : Exception
    {
        public UnhandledUserException(string s, Exception exception) : base(s, exception) {}
    }
}