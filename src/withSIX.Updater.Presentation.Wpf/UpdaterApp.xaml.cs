// <copyright company="SIX Networks GmbH" file="UpdaterApp.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using withSIX.Core.Presentation.Wpf.Services;

namespace withSIX.Updater.Presentation.Wpf
{
    public partial class UpdaterApp : Application
    {
        public UpdaterApp() {
#if !DEBUG
            ExDialog.SetupExceptionHandler(this);
#endif
        }

        public static void Launch() {
            var application = new UpdaterApp {ShutdownMode = ShutdownMode.OnMainWindowClose};
            application.InitializeComponent();
            application.Run();
        }
    }
}