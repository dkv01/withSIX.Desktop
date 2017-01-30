// <copyright company="SIX Networks GmbH" file="IDialogManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace withSIX.Core.Applications.Services
{
    public interface ISpecialDialogManager
    {
        Task ShowPopup(object vm, IDictionary<string, object> overrideSettings = null);
        Task ShowWindow(object vm, IDictionary<string, object> overrideSettings = null);
        Task<bool?> ShowDialog(object vm, IDictionary<string, object> overrideSettings = null);
        Task<Tuple<string, string, bool?>> UserNamePasswordDialog(string pleaseEnterUsernameAndPassword, string location);
        Task<Tuple<SixMessageBoxResult, string>> ShowEnterConfirmDialog(string msg, string defaultInput);
        Task<SixMessageBoxResult> MessageBox(MessageBoxDialogParams dialogParams);
    }


    /// <summary>
    /// Re-usable dialogs
    /// </summary>
    // TODO: Make this an async api!!
    public interface IDialogManager
    {
        Task<string> BrowseForFolder(string selectedPath = null, string title = null);

        Task<string> BrowseForFile(string initialDirectory = null, string title = null, string defaultExt = null,
            bool checkFileExists = true);

        Task<SixMessageBoxResult> MessageBox(MessageBoxDialogParams dialogParams);

        Task<bool> ExceptionDialog(Exception e, string message,
            string title = null, object owner = null);
    }
}