// <copyright company="SIX Networks GmbH" file="RuntimeCheck.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SN.withSIX.Core.Presentation.Bridge
{
    public class RuntimeCheck
    {
        static readonly Uri net461 = new Uri("https://www.microsoft.com/en-us/download/details.aspx?id=49981");

        static readonly Version seven = new Version("6.1");

        public async Task Check() {
            await CheckNet46().ConfigureAwait(false);
            var legacyCheck = RuntimePolicyHelper.LegacyV2RuntimeEnabledSuccessfully;
        }

        async Task CheckNet46() {
            if (IsNet46OrNewer())
                return;

            if (!IsSevenOrNewer()) {
                await
                    FatalErrorMessage(
                        "Windows 7 or later is required due to .NET framework 4.6 support and/or browser components.",
                        "Windows 7 or later required").ConfigureAwait(false);
                Environment.Exit(1);
            }

            if (
                await
                    FatalErrorMessage(
                        ".NET framework 4.6 or later is required, but was not found.\n\nDo you want to install it now?",
                        ".NET framework 4.6 or later required").ConfigureAwait(false))
                TryOpenNet46Url();
            Environment.Exit(1);
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

        static bool IsSevenOrNewer() => Environment.OSVersion.Version >= seven;

        static bool IsNet46OrNewer() => Get46FromRegistry();

        static bool Get46FromRegistry() {
            using (
                var ndpKey =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                        .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\")) {
                return (ndpKey?.GetValue("Release") != null) && CheckFor46DotVersion((int) ndpKey.GetValue("Release"));
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
                        typeof(ICLRRuntimeInfo).GUID);
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
}