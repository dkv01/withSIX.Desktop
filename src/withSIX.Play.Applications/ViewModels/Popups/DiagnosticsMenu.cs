// <copyright company="SIX Networks GmbH" file="DiagnosticsMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;

using withSIX.Core;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.MVVM.Attributes;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Core.Presentation.Wpf.Extensions;

namespace withSIX.Play.Applications.ViewModels.Popups
{
    public class DiagnosticsMenu : ContextMenuBase
    {
        readonly IDialogManager _dialogManager;
        readonly IRestarter _restarter;

        public DiagnosticsMenu(IDialogManager dialogManager, IRestarter restarter) {
            _dialogManager = dialogManager;
            _restarter = restarter;

            if (Common.Flags.Verbose)
                Items.Remove(GetItem(RestartWithDiagnostics));
        }

        [MenuItem]

        public void OpenLog() {
            Tools.FileUtil.OpenFolderInExplorer(Common.Paths.LogPath.ToString());
        }

        [MenuItem]

        public void OpenConfig() {
            Tools.FileUtil.OpenFolderInExplorer(Common.Paths.DataPath.ToString());
        }

        [MenuItem]

        public void RestartWithDiagnostics() {
            if (
                _dialogManager.MessageBox(
                    new MessageBoxDialogParams("Restarting the client in diagnostic logging mode, are you sure?",
                        "Restart to enable diagnostics?", SixMessageBoxButton.YesNo)).WaitSpecial().IsYes()) {
                _restarter.RestartWithoutElevation(
                    Environment.GetCommandLineArgs().Skip(1).Concat(new[] {"--verbose"}).ToArray());
            }
        }

        [MenuItem]

        public async Task SaveLogs() {
            var path =
                Common.Paths.TempPath.GetChildFileWithName("Play withSIX diagnostics " + DateTime.UtcNow.ToFileTimeUtc() +
                                                           ".zip");
            await ErrorHandlerr.GenerateDiagnosticZip(path).ConfigureAwait(false);
            Tools.FileUtil.SelectInExplorer(path.ToString());
        }
    }
}