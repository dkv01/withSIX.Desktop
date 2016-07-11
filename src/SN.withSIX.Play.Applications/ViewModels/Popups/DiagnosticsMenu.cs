// <copyright company="SIX Networks GmbH" file="DiagnosticsMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;

using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.MVVM.Attributes;
using SN.withSIX.Core.Applications.MVVM.ViewModels;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;

namespace SN.withSIX.Play.Applications.ViewModels.Popups
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
                        "Restart to enable diagnostics?", SixMessageBoxButton.YesNo)).WaitAndUnwrapException().IsYes()) {
                _restarter.RestartWithoutElevation(
                    Tools.Generic.GetStartupParameters().Concat(new[] {"--verbose"}).ToArray());
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