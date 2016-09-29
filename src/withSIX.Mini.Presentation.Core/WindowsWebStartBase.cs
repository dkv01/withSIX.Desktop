// <copyright company="SIX Networks GmbH" file="WindowsWebStartBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Services;
using withSIX.Core.Services.Infrastructure;
using withSIX.Mini.Applications;
using withSIX.Mini.Presentation.Core.Services;

namespace withSIX.Mini.Presentation.Core
{
    [Obsolete("Not needed when using Kestrel", true)]
    public abstract class WindowsWebStartBase
    {
        private readonly IDialogManager _dm;
        private readonly IExitHandler _exitHandler;
        private readonly IProcessManager _pm;

        protected WindowsWebStartBase(IProcessManager pm, IDialogManager dm, IExitHandler exitHandler) {
            _pm = pm;
            _dm = dm;
            _exitHandler = exitHandler;
        }

        public Task Initialize() {
            TryHandlePorts();

            // TODO: ON startup or at other times too??
            return TaskExt.Default;
        }

        public Task Deinitialize() => TaskExt.Default;

        private void TryHandlePorts() {
            try {
                HandleSystem();
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
            } catch (OperationCanceledException ex) {
                MainLog.Logger.FormattedFatalException(ex, "Failure setting up API ports");
                // TODO: Throw instead?
                _dm.MessageBox(
                    new MessageBoxDialogParams(
                        "Configuration of API ports are required but were cancelled by the user.\nPlease allow the Elevation prompt on retry. The application is now closing.",
                        "Sync: API Ports Configuration cancelled"));
                _exitHandler.Exit(1);
            }
        }

        private void HandleSystem() {
            var si = BuildSi(_pm);
            if (((Consts.HttpAddress == null) || si.IsHttpPortRegistered) &&
                ((Consts.HttpsAddress == null) || si.IsSslRegistered()))
                return;
            WindowsApiPortHandler.SetupApiPort(Consts.HttpAddress, Consts.HttpsAddress, _pm);
            si = BuildSi(_pm); // to output
        }

        public static ServerInfo BuildSi(IProcessManagerSync pm)
            => new ServerInfo(pm, Consts.HttpAddress, Consts.HttpsAddress);
    }
}