// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Infra.Api.Messengers;
using SN.withSIX.Steam.Api;
using SN.withSIX.Steam.Core;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Infra.Api
{
    public static class IPEndPointExtensions
    {
        public static string ToHttp(this IPEndPoint ep) => ToProto(ep, "http");

        public static string ToHttps(this IPEndPoint ep) => ToProto(ep, "https");
        private static string ToProto(IPEndPoint ep, string scheme) => scheme + "://" + ep;
    }

    public class Initializer : IInitializer, IInitializeAfterUI
    {
        static IDisposable _webServer;
        private readonly IWebApiErrorHandler _errorHandler;
        private readonly IDbContextFactory _factory;
        private readonly IProcessManager _pm;
        private readonly IDialogManager _dm;
        private readonly IStateMessengerBus _stateMessenger;
        private IDisposable _ErrorReg;

        public Initializer(IWebApiErrorHandler errorHandler, IDbContextFactory factory,
            IStateMessengerBus stateMessenger, IProcessManager pm, IDialogManager dm) {
            _errorHandler = errorHandler;
            _factory = factory;
            _stateMessenger = stateMessenger;
            _pm = pm;
            _dm = dm;
        }

        public async Task InitializeAfterUI() {
            // We have to run this outside of a DB scope
            // If we don't, then the AmbientScopeIdentifier will be inherited into the Web/SIR requests
            // and remain there even when we close the scope.
            using (_factory.SuppressAmbientContext()) {
                await TryLaunchWebserver().ConfigureAwait(false);
                _ErrorReg = UserError.RegisterHandler(error => _errorHandler.Handler(error));
            }
        }

        public Task Initialize() {
            _stateMessenger.Initialize();

            TryHandlePorts();

            // TODO: ON startup or at other times too??
            return TaskExt.Default;
        }

        private void TryHandlePorts() {
            try {
                HandleSystem();
            } catch (OperationCanceledException ex) {
                MainLog.Logger.FormattedFatalException(ex, "Failure setting up API ports");
                // TODO: Throw instead?
                _dm.MessageBox(
                    new MessageBoxDialogParams(
                        "Configuration of API ports are required but were cancelled by the user.\nPlease allow the Elevation prompt on retry. The application is now closing.",
                        "Sync: API Ports Configuration cancelled"));
                Environment.Exit(1);
            }
        }

        private void HandleSystem() {
            var si = Infra.Api.Initializer.BuildSi(_pm);
            if (((Consts.HttpAddress == null) || si.IsHttpPortRegistered) &&
                ((Consts.HttpsAddress == null) || si.IsSslRegistered()))
                return;
            ApiPortHandler.SetupApiPort(Consts.HttpAddress, Consts.HttpsAddress, _pm);
            si = Infra.Api.Initializer.BuildSi(_pm); // to output
        }


        // This requires the Initializer to be a singleton, not great to have to require singleton for all?
        public Task Deinitialize() {
            _webServer?.Dispose();
            _webServer = null;
            _ErrorReg?.Dispose();
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

            var si = BuildSi(_pm);
            if (!si.IsHttpsPortRegistered && !si.IsHttpPortRegistered)
                throw new InvalidOperationException("Neither http nor https ports are registered");
            if (si.IsHttpsPortRegistered && !si.IsCertRegistered)
                throw new InvalidOperationException("The certificate failed to register");

            if (!si.IsHttpPortRegistered)
                http = null;
            if (!si.IsSslRegistered())
                https = null;

            retry:
            try {
                _webServer = Startup.Start(http, https);
            } catch (TargetInvocationException ex) {
                var unwrapped = ex.UnwrapExceptionIfNeeded();
                if (!(unwrapped is HttpListenerException))
                    throw;

                if (tries++ >= maxTries)
                    throw GetCustomException(unwrapped, https ?? http);
                MainLog.Logger.Warn(unwrapped.Format());
                Thread.Sleep(timeOut);
                goto retry;
            } catch (HttpListenerException ex) {
                if (tries++ >= maxTries)
                    throw GetCustomException(ex, https ?? http);
                MainLog.Logger.FormattedWarnException(ex);
                Thread.Sleep(timeOut);
                goto retry;
            }
        }

        public static ServerInfo BuildSi(IProcessManagerSync pm)
            => new ServerInfo(pm, Consts.HttpAddress, Consts.HttpsAddress);

        static Exception GetCustomException(Exception unwrapped, IPEndPoint addr)
            => new CannotOpenApiPortException("The address: " + addr + " is already in use?\n" + unwrapped.Message,
                unwrapped);
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

    public class CannotOpenApiPortException : Exception
    {
        public CannotOpenApiPortException(string message) : base(message) {}
        public CannotOpenApiPortException(string message, Exception ex) : base(message, ex) {}
    }
}