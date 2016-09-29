// <copyright company="SIX Networks GmbH" file="WebInitializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using ReactiveUI;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Logging;
using withSIX.Core.Services.Infrastructure;
using withSIX.Mini.Infra.Api.Messengers;

namespace withSIX.Mini.Infra.Api
{
    public class WebInitializer : IInitializer, IInitializeAfterUI
    {
        private readonly IWebApiErrorHandler _errorHandler;
        private readonly IStateMessengerBus _stateMessenger;
        private IDisposable _ErrorReg;

        public WebInitializer(IWebApiErrorHandler errorHandler, IStateMessengerBus stateMessenger) {
            _errorHandler = errorHandler;
            _stateMessenger = stateMessenger;
        }

        public async Task InitializeAfterUI() {
            _ErrorReg = UserError.RegisterHandler(error => _errorHandler.Handler(error));
        }

        public Task Initialize() {
            _stateMessenger.Initialize();

            // TODO: ON startup or at other times too??
            return TaskExt.Default;
        }


        // This requires the Initializer to be a singleton, not great to have to require singleton for all?
        public Task Deinitialize() {
            _ErrorReg?.Dispose();
            return TaskExt.Default;
        }
    }

    public class ServerInfo
    {
        public ServerInfo(IProcessManagerSync pm, IPEndPoint http, IPEndPoint https) {
            IsHttpPortRegistered = (http != null) && QueryPortRegistered(pm, http.ToHttp());
            IsHttpsPortRegistered = (https != null) && QueryPortRegistered(pm, https.ToHttps());
            IsCertRegistered = QueryCertRegistered(pm, https);
            MainLog.Logger.Info(
                $"HttpRegistered: {IsHttpPortRegistered} ({http}), HttpsRegistered: {IsHttpsPortRegistered} ({https}), CertRegistered: {IsCertRegistered}");
        }

        public bool IsHttpPortRegistered { get; }
        public bool IsHttpsPortRegistered { get; }
        public bool IsCertRegistered { get; }

        public bool IsSslRegistered() => IsHttpsPortRegistered && IsCertRegistered;

        static bool QueryPortRegistered(IProcessManagerSync pm, string value) {
            var output = pm.LaunchAndGrabToolCmd(new ProcessStartInfo("netsh", "http show urlacl"), "netsh");
            return output.StandardOutput.Contains(value)
                   || output.StandardError.Contains(value);
        }

        static bool QueryCertRegistered(IProcessManagerSync pm, IPEndPoint value) {
            var output = pm.LaunchAndGrabToolCmd(new ProcessStartInfo("netsh", "http show sslcert"), "netsh");
            var epStr = value.ToString();
            return output.StandardOutput.Contains(epStr)
                   || output.StandardError.Contains(epStr);
        }
    }
}