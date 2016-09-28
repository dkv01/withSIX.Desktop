// <copyright company="SIX Networks GmbH" file="RuntimeCheck.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace SN.withSIX.Core.Presentation.Wpf.Services
{

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

            SetForeground(Tools.ProcessManager.Management.FindProcess(process));
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