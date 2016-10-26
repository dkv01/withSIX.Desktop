// <copyright company="SIX Networks GmbH" file="EntryPoint.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using withSIX.Core;
using withSIX.Core.Logging;
using withSIX.Core.Services;

namespace withSIX.Updater.Presentation.Wpf
{
    public class EntryPoint
    {
        const string AppName = "SIX Updater";

        [STAThread]
        public static void Main() {
            try {
                Initialize();
            } catch (Exception ex) {
                TryLogException(ex);
                throw;
            }
        }

        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);

        static void Initialize() {
            AttachConsole(-1);
            CommonBase.AssemblyLoader = new AssemblyLoader(Assembly.GetEntryAssembly());
            // TODO
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            StartupSequence.PreInit(AppName);
            UpdaterApp.Launch();
            if (Common.OnExit != null)
                Common.OnExit();
        }

        static void TryLogException(Exception ex) {
            try {
                MainLog.Logger.FormattedErrorException(ex, "Abnormal termination");
            } catch {}
        }
    }
}