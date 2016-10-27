// <copyright company="SIX Networks GmbH" file="NodeDialogManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;

namespace withSIX.Mini.Presentation.Electron
{
    public class NodeDialogManager : IDialogManager
    {
        private readonly INodeApi _api;

        public NodeDialogManager(INodeApi api) {
            _api = api;
        }

        public async Task<string> BrowseForFolder(string selectedPath = null, string title = null)
            => (await _api.ShowFolderDialog(title, selectedPath).ConfigureAwait(false))?[0];

        public async Task<string> BrowseForFile(string initialDirectory = null, string title = null,
                string defaultExt = null,
                bool checkFileExists = true)
            =>
            checkFileExists
                ? (await _api.ShowFileDialog(title, initialDirectory).ConfigureAwait(false))?[0]
                : await _api.ShowSaveDialog(title, initialDirectory).ConfigureAwait(false);

        public async Task<bool> ExceptionDialog(Exception e, string message, string title = null, object window = null) {
            await _api.ShowMessageBox(title, message + "\n" + e.Format(), null, "error").ConfigureAwait(false);
            return true;
        }

        public async Task<SixMessageBoxResult> MessageBox(MessageBoxDialogParams dialogParams) {
            var r =
                await
                    _api.ShowMessageBox(dialogParams.Title, dialogParams.Message, GetButtons(dialogParams))
                        .ConfigureAwait(false);
            return (SixMessageBoxResult) Enum.Parse(typeof(SixMessageBoxResult), r);
        }

        private static string[] GetButtons(MessageBoxDialogParams dialogParams) {
            switch (dialogParams.Buttons) {
            case SixMessageBoxButton.OK: {
                return new[] {"OK"};
            }
            case SixMessageBoxButton.OKCancel: {
                return new[] {"OK", "Cancel"};
            }
            case SixMessageBoxButton.YesNo: {
                return new[] {"Yes", "No"};
            }
            case SixMessageBoxButton.YesNoCancel: {
                return new[] {"Yes", "No", "Cancel"};
            }
            }
            throw new NotSupportedException("Unsupported messagebox option");
        }
    }
}