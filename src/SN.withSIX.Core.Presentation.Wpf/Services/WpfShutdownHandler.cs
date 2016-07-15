// <copyright company="SIX Networks GmbH" file="WpfShutdownHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Presentation.Services;

namespace SN.withSIX.Core.Presentation.Wpf.Services
{
    public class WpfShutdownHandler : IShutdownHandler
    {
        readonly IExitHandler _exitHandler;

        public WpfShutdownHandler(IExitHandler exitHandler) {
            _exitHandler = exitHandler;
        }

        public void Shutdown(int exitCode) {
            var app = Application.Current;

            Common.Flags.ShuttingDown = true;

            if (app != null)
                app.Dispatcher.Invoke(() => app.Shutdown(exitCode));
            else
                _exitHandler.Exit(exitCode);
        }
    }
}