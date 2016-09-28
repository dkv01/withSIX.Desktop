// <copyright company="SIX Networks GmbH" file="ExplorerExtensionInstaller.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Presentation.Core.Services
{
    public class ProcessExitException : Exception
    {
        public ProcessExitException() {}

        public ProcessExitException(string message) : base(message) {}

        public ProcessExitException(string message, Exception innerException) : base(message, innerException) {}

        public int ExitCode { get; set; }
    }

    public class ExplorerExtensionInstaller : IExplorerExtensionInstaller, IPresentationService
    {
        const int WM_USER = 0x0400; //http://msdn.microsoft.com/en-us/library/windows/desktop/ms644931(v=vs.85).aspx

        public async Task UpgradeOrInstall(IAbsoluteDirectoryPath destination, Settings settings,
            params IAbsoluteFilePath[] files) {
            var theShell = GetTheShell(destination, files);
            if (theShell.Exists)
                await Uninstall(destination, settings, files).ConfigureAwait(false);
            await Install(destination, settings, files).ConfigureAwait(false);
        }

        public async Task Install(IAbsoluteDirectoryPath destination, Settings settings,
            params IAbsoluteFilePath[] files) {
            destination.MakeSurePathExists();
            foreach (var f in files)
                await f.CopyAsync(destination).ConfigureAwait(false);
            var theShell = GetTheShell(destination, files);
            RunSrm(theShell, "install", "-codebase");
            await RestartExplorer().ConfigureAwait(false);
            settings.ExtensionInstalled();
        }

        public async Task Uninstall(IAbsoluteDirectoryPath destination, Settings settings,
            params IAbsoluteFilePath[] files) {
            var theShell = GetTheShell(destination, files);
            try {
                RunSrm(theShell, "uninstall");
                await RestartExplorer().ConfigureAwait(false);
                foreach (var d in files.Select(f => destination.GetChildFileWithName(f.FileName)).Where(d => d.Exists))
                    d.Delete();
            } catch (ProcessExitException ex) {
                // Already uninstalled..
                if (ex.ExitCode != 255)
                    throw;
            }
            settings.ExtensionUninstalled();
        }

        private static IAbsoluteFilePath GetTheShell(IAbsoluteDirectoryPath destination, IAbsoluteFilePath[] files)
            => destination.GetChildFileWithName(files.First().FileName);

        private static void RunSrm(IAbsoluteFilePath theShell, string command, string additional = null) {
            try {
                using (
                    var p = Process.Start("srm.exe",
                        command + " \"" + theShell + "\"" + (additional == null ? null : " " + additional))) {
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                        throw new ProcessExitException("srm exited with code: " + p.ExitCode) {ExitCode = p.ExitCode};
                }
            } catch (Win32Exception ex) {
                if (ex.IsElevationCancelled())
                    throw ex.HandleUserCancelled();
                throw;
            }
        }

        // http://stackoverflow.com/questions/3939832/how-to-update-windows-explorers-shell-extension-without-rebooting
        // http://winaero.com/blog/how-to-properly-restart-the-explorer-shell-in-windows/
        /*        private void RestartExplorer() {
                    using (var p = Process.Start("cmd.exe", "/C \"taskkill /IM explorer.exe /F\""))
                        p.WaitForExit();
                    using (var p = Process.Start("explorer.exe"))
                        ;
                }*/

        // http://stackoverflow.com/questions/565405/how-to-programatically-restart-windows-explorer-process
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, [MarshalAs(UnmanagedType.U4)] uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        static Task RestartExplorer() => TaskExt.StartLongRunningTask(() => RestartExplorerInternal());

        static void RestartExplorerInternal() {
            try {
                var ptr = FindWindow("Shell_TrayWnd", null);
                //Console.WriteLine("INIT PTR: {0}", ptr.ToInt32());
                PostMessage(ptr, WM_USER + 436, (IntPtr) 0, (IntPtr) 0);

                do {
                    ptr = FindWindow("Shell_TrayWnd", null);
                    //Console.WriteLine("PTR: {0}", ptr.ToInt32());

                    if (ptr.ToInt32() == 0) {
                        //Console.WriteLine("Success. Breaking out of loop.");
                        break;
                    }

                    Thread.Sleep(1000);
                } while (true);
            } catch (Exception ex) {
                //Console.WriteLine("{0} {1}", ex.Message, ex.StackTrace);
            }
            //Console.WriteLine("Restarting the shell.");
            string explorer = $"{Environment.GetEnvironmentVariable("WINDIR")}\\{"explorer.exe"}";
            using (var process = new Process {
                StartInfo = {
                    FileName = explorer,
                    UseShellExecute = true
                }
            })
                process.Start();
        }
    }
}