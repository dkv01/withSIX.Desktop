// <copyright company="SIX Networks GmbH" file="ConsoleDialogManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Presentation;

namespace withSIX.Mini.Presentation.CoreCore.Services
{
    // TODO: Node :-)
    public class ConsoleDialogManager : IDialogManager, IPresentationService
    {
        public Task<string> BrowseForFolder(string selectedPath = null, string title = null) {
            throw new NotImplementedException();
        }

        public Task<string> BrowseForFile(string initialDirectory = null, string title = null, string defaultExt = null,
            bool checkFileExists = true) {
            throw new NotImplementedException();
        }

        public Task<SixMessageBoxResult> MessageBox(MessageBoxDialogParams dialogParams) {
            throw new NotImplementedException();
        }

        public Task<bool> ExceptionDialog(Exception e, string message, string title = null, object owner = null) {
            throw new NotImplementedException();
        }
    }
}