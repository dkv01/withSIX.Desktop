// <copyright company="SIX Networks GmbH" file="FlashHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation.Assemblies;
using SN.withSIX.Core.Presentation.Wpf.Helpers;

namespace SN.withSIX.Core.Presentation.Wpf.Services
{
    public class FlashHandler
    {
        readonly Uri _flashUri;

        public FlashHandler(Uri flashUri) {
            _flashUri = flashUri;
        }

        public async Task InstallFlash() {
            try {
                await TryInstallFlash().ConfigureAwait(false);
            } catch (Exception e) {
                MainLog.Logger.FormattedWarnException(e, "Error while installing flash");
                /*                MessageBox.Show(
                    string.Format(
                        "Failed installing pre-requisites, please make sure you are connected to the internet:\n"
                        +
                        "You can try install manually from: http://www.adobe.com/support/flashplayer/downloads.html \n\n"
                        + "Error details: {0}: {1}", e.GetType(), e.Message)
                    + "\n\nFor Support please visit withsix.com/support");*/
            }
        }

        async Task<bool> TryInstallFlash() {
            var installer = new FlashInstaller(Path.GetTempPath(), _flashUri);
            if (installer.IsInstalled())
                return false;

            using (BuildPreRequisiteSplashScreen())
                await installer.Install().ConfigureAwait(false);
            return true;
        }

        static SplashScreenHandler BuildPreRequisiteSplashScreen()
            => new SplashScreenHandler(new SplashScreen("PrerequisitesInstalling.png"));
    }
}