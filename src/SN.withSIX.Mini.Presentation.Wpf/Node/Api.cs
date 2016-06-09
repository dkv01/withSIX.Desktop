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
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Applications.Usecases.Settings;

namespace SN.withSIX.Mini.Presentation.Wpf.Node
{
    public class Api : IUsecaseExecutor
    {
        public Task<object> Invoke(dynamic input) {
            var request = (string) input.request;
            var requestData = input.data ?? new Dictionary<string, object>();

            switch (request) {
            case "getSettings": {
                return Request<GetGeneralSettings, GeneralSettings>(requestData);
            }
            case "saveSettings": {
                return Request<SaveGeneralSettings, UnitType>(requestData);
            }
            case "saveLogs": {
                return Request<SaveLogs, UnitType>(requestData);
            }
            case "enableDiagnostics": {
                return Request<StartInDiagnosticsMode, UnitType>(requestData);
            }
            case "installExplorerExtension": {
                return Request<InstallExtension, UnitType>(requestData);
            }
            case "uninstallExplorerExtension": {
                return Request<RemoveExtension, UnitType>(requestData);
            }
            default: {
                throw new Exception("Unknown command");
            }
            }
        }

        async Task<object> Request<T, T2>(dynamic requestData) where T : IAsyncRequest<T2> {
            var data = SerializationExtension.ToJson(requestData);
            T request = Tools.Serialization.Json.LoadJson<T>(data);
            //Console.WriteLine("Calling {0}, with data: {1}, as request: {2}. MEdiator: {3}", typeof(T), data, request, Cheat.Mediator);
            return await this.RequestAsync(request).ConfigureAwait(false);
        }
    }
}