// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;

namespace SN.withSIX.Mini.Presentation.Wpf
{
    public class Initializer : IInitializer
    {
        private readonly string[] _schemes = {"syncws"};
        readonly IToolsInstaller _toolsInstaller;

        public Initializer(IToolsInstaller toolsInstaller) {
            _toolsInstaller = toolsInstaller;
        }

        public async Task Initialize() {
            RegisterUrlHandlers();
            //await SetupNotificationIcon().ConfigureAwait(false);
        }

        public async Task Deinitialize() {}

        async Task SetupNotificationIcon() {
            var ps1 = Path.GetTempFileName() + ".ps1";
            var assembly = GetType().Assembly; // Executing assembly??
            using (var fs = new FileStream(ps1, FileMode.Create))
            using (
                var s =
                    assembly.GetManifestResourceStream("SN.withSIX.Mini.Presentation.Wpf.Resources.TrayIcon.ps1")
                )
                await s.CopyToAsync(fs).ConfigureAwait(false);
            using (var p =
                ReactiveProcess.Create("powershell.exe", "-File \"" + ps1 + "\" '" + assembly.Location + "' 2"))
                await p.StartAsync().ConfigureAwait(false);
        }

        public void RegisterUrlHandlers() {
            foreach (var protocol in _schemes)
                RegisterProtocol(protocol);
        }

        static void RegisterProtocol(string protocol) {
            var key = Registry.CurrentUser.CreateSubKey(Path.Combine(Common.AppCommon.Classes, protocol));
            key.SetValue(string.Empty, "URL:Sync withSIX " + protocol + " Protocol");
            key.SetValue("URL Protocol", string.Empty);

            var iconKey = key.CreateSubKey("DefaultIcon");
            iconKey.SetValue(string.Empty, Common.Paths.EntryLocation + ",1");

            key = key.CreateSubKey(@"shell\open\command");
            key.SetValue(string.Empty, Common.Paths.EntryLocation.EscapePath() + " \"%1\"");
        }

        public void UnregisterUrlHandlers() {
            foreach (var protocol in _schemes)
                UnregisterProtocol(protocol);
        }

        private static void UnregisterProtocol(string protocol)
            => Registry.CurrentUser.DeleteSubKey(Path.Combine(Common.AppCommon.Classes, protocol), false);
    }
}