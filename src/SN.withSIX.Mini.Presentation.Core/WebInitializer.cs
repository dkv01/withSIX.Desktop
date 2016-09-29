// <copyright company="SIX Networks GmbH" file="WebInitializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Services.Infra;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Presentation.Core
{
    public class WebInitializer : IInitializer, IInitializeAfterUI
    {
        static IDisposable _webServer;
        private readonly IDbContextFactory _factory;
        private readonly IWebServerStartup _webServerStartup;

        public WebInitializer(IDbContextFactory factory, IWebServerStartup webServerStartup) {
            _factory = factory;
            _webServerStartup = webServerStartup;
        }

        public async Task InitializeAfterUI() {
            // We have to run this outside of a DB scope
            // If we don't, then the AmbientScopeIdentifier will be inherited into the Web/SIR requests
            // and remain there even when we close the scope.
            using (_factory.SuppressAmbientContext()) {
                await TryLaunchWebserver().ConfigureAwait(false);
            }
        }

        public Task Initialize() {
            // TODO: ON startup or at other times too??
            return TaskExt.Default;
        }


        // This requires the Initializer to be a singleton, not great to have to require singleton for all?
        public Task Deinitialize() {
            var ws = _webServer;
            if (ws != null)
                Task.Run(() => ws.Dispose()); // TODO: This currently locks up! so we run it in a task :S
            _webServer = null;
            return TaskExt.Default;
        }

        async Task TryLaunchWebserver() {
            retry:
            try {
                SetupWebServer();
            } catch (CannotOpenApiPortException ex) {
                var r = await
                    UserErrorHandler.RecoverableUserError(ex, "Unable to open required ports",
                            "We were unable to open the required port for the website to communicate with the client.\nAre there other instances already running on your system?\n\nIf you continue to experience this problem please contact support @ https://community.withsix.com")
                        .ConfigureAwait(false);
                if (r == RecoveryOptionResultModel.RetryOperation)
                    goto retry;
                throw;
            }
        }

        void SetupWebServer() {
            const int maxTries = 10;
            const int timeOut = 1500;
            var tries = 0;

            var http = Consts.HttpAddress;
            var https = Consts.HttpsAddress;
            retry:
            try {
                _webServer = _webServerStartup.Start(http, https);
            } catch (ListenerException ex) {
                if (tries++ >= maxTries)
                    throw GetCustomException(ex, https ?? http);
                MainLog.Logger.FormattedWarnException(ex);
                Thread.Sleep(timeOut);
                goto retry;
            }
        }

        static Exception GetCustomException(Exception unwrapped, IPEndPoint addr)
            => new CannotOpenApiPortException("The address: " + addr + " is already in use?\n" + unwrapped.Message,
                unwrapped);
    }

    public class ListenerException : Exception
    {
        public ListenerException(string message, Exception innerException) : base(message, innerException) {}
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

    public interface IWebServerStartup
    {
        IDisposable Start(IPEndPoint http, IPEndPoint https);
    }
}