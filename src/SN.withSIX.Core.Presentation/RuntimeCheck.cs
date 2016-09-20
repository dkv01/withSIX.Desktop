// <copyright company="SIX Networks GmbH" file="RuntimeCheck.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SN.withSIX.Core.Presentation
{
    public class RuntimeCheck
    {
        static readonly Uri net461 = new Uri("https://www.microsoft.com/en-us/download/details.aspx?id=49981");

        public async Task Check() {
            await CheckNet46().ConfigureAwait(false);
            var legacyCheck = RuntimePolicyHelper.LegacyV2RuntimeEnabledSuccessfully;
        }

        async Task CheckNet46() {
            if (IsNet46OrNewer())
                return;

            if (!IsSevenOrNewer()) {
                await FatalErrorMessage("Windows 7 or later is required due to .NET framework 4.6 support and/or browser components.", "Windows 7 or later required").ConfigureAwait(false);
                System.Environment.Exit(1);
            }

            if (await FatalErrorMessage(".NET framework 4.6 or later is required, but was not found.\n\nDo you want to install it now?", ".NET framework 4.6 or later required").ConfigureAwait(false))
                TryOpenNet46Url();
            System.Environment.Exit(1);
        }

        protected virtual async Task<bool> FatalErrorMessage(string message, string caption) {
            Console.WriteLine(caption + ": " + message + "\nY/N");
            var key = Console.ReadKey();
            return key.Key.ToString().ToLower() == "y";
        }

        static void TryOpenNet46Url() {
            try {
                Process.Start(net461.ToString());
            } catch (Exception) {}
        }

        static readonly Version seven = new Version("6.1");

        static bool IsSevenOrNewer() => System.Environment.OSVersion.Version >= seven;

        static bool IsNet46OrNewer() => Get46FromRegistry();

        static bool Get46FromRegistry() {
            using (
                var ndpKey =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                        .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\")) {
                return ndpKey?.GetValue("Release") != null && CheckFor46DotVersion((int) ndpKey.GetValue("Release"));
            }
        }

        // Checking the version using >= will enable forward compatibility, 
        // however you should always compile your code on newer versions of
        // the framework to ensure your app works the same.
        private static bool CheckFor46DotVersion(int releaseKey) {
            // 4.6 or later
            if (releaseKey >= 393295)
                return true;
            return false;
            /*
            if ((releaseKey >= 379893)) {
                return "4.5.2 or later";
            }
            if ((releaseKey >= 378675)) {
                return "4.5.1 or later";
            }
            if ((releaseKey >= 378389)) {
                return "4.5 or later";
            }
            // This line should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
            */
        }

        static class RuntimePolicyHelper
        {
            static RuntimePolicyHelper() {
                var clrRuntimeInfo =
                    (ICLRRuntimeInfo) RuntimeEnvironment.GetRuntimeInterfaceAsObject(
                        Guid.Empty,
                        typeof (ICLRRuntimeInfo).GUID);
                TryGetRuntimePolicy(clrRuntimeInfo);
            }

            public static bool LegacyV2RuntimeEnabledSuccessfully { get; private set; }

            static void TryGetRuntimePolicy(ICLRRuntimeInfo clrRuntimeInfo) {
                try {
                    clrRuntimeInfo.BindAsLegacyV2Runtime();
                    LegacyV2RuntimeEnabledSuccessfully = true;
                } catch (COMException) {
                    // This occurs with an HRESULT meaning 
                    // "A different runtime was already bound to the legacy CLR version 2 activation policy."
                    LegacyV2RuntimeEnabledSuccessfully = false;
                }
            }

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891")]
            interface ICLRRuntimeInfo
            {
                void xGetVersionString();
                void xGetRuntimeDirectory();
                void xIsLoaded();
                void xIsLoadable();
                void xLoadErrorString();
                void xLoadLibrary();
                void xGetProcAddress();
                void xGetInterface();
                void xSetDefaultStartupFlags();
                void xGetDefaultStartupFlags();

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void BindAsLegacyV2Runtime();
            }
        }
    }
    #region Nested type: NativeMethods

    public class NativeMethods
    {
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_RESTORE = 9;

        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int EnumWindows(Action<int, int> x, int y);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        static void SetForeground(string process) {
            Contract.Requires<ArgumentNullException>(process != null);
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(process));

            SetForeground(Tools.Processes.FindProcess(process));
        }

        public static void SetForeground(Process process, int cmdShow = SW_SHOWNORMAL) {
            Contract.Requires<ArgumentNullException>(process != null);

            ShowWindow(process.MainWindowHandle, cmdShow);
            SetForegroundWindow(process.MainWindowHandle);
        }

        public static void MinimizeWindow(Process process) {
            Contract.Requires<ArgumentNullException>(process != null);

            ShowWindow(process.MainWindowHandle, SW_SHOWMINIMIZED);
        }

        static void MaximizeWindow(Process process) {
            Contract.Requires<ArgumentNullException>(process != null);

            ShowWindow(process.MainWindowHandle, SW_SHOWMAXIMIZED);
        }

        static void ShowWindow(Process process) {
            Contract.Requires<ArgumentNullException>(process != null);
            ShowWindow(process.MainWindowHandle, SW_SHOWNORMAL);
        }

        static void SetForeground(Process[] processes) {
            Contract.Requires<ArgumentNullException>(processes != null);

            foreach (var p in processes)
                SetForeground(p);
        }
    }

    #endregion

    sealed class Win32
    {
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void HideConsoleWindow() {
            var hWnd = Process.GetCurrentProcess().MainWindowHandle;
            if (hWnd != IntPtr.Zero)
                ShowWindow(hWnd, 0); // 0 = SW_HIDE
        }
    }
}