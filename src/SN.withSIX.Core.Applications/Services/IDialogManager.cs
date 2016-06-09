// <copyright company="SIX Networks GmbH" file="IDialogManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using SmartAssembly.Attributes;

namespace SN.withSIX.Core.Applications.Services
{
    [ContractClass(typeof (SpecialDialogManagerContract))]
    public interface ISpecialDialogManager
    {
        Task ShowPopup(object vm, IDictionary<string, object> overrideSettings = null);
        Task ShowWindow(object vm, IDictionary<string, object> overrideSettings = null);
        Task<bool?> ShowDialog(object vm, IDictionary<string, object> overrideSettings = null);
        Task<Tuple<string, string, bool?>> UserNamePasswordDialog(string pleaseEnterUsernameAndPassword, string location);
        Task<Tuple<SixMessageBoxResult, string>> ShowEnterConfirmDialog(string msg, string defaultInput);
        Task<SixMessageBoxResult> MessageBox(MessageBoxDialogParams dialogParams);
    }

    [DoNotObfuscate]
    [ContractClass(typeof (DialogManagerContract))]
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

    [ContractClassFor(typeof (ISpecialDialogManager))]
    public abstract class SpecialDialogManagerContract : ISpecialDialogManager
    {
        public Task<bool?> ShowDialog(object vm, IDictionary<string, object> overrideSettings = null) {
            Contract.Requires<ArgumentNullException>(vm != null);
            return default(Task<bool?>);
        }

        public Task<Tuple<string, string, bool?>> UserNamePasswordDialog(string pleaseEnterUsernameAndPassword,
            string location) => default(Task<Tuple<string, string, bool?>>);

        public Task<Tuple<SixMessageBoxResult, string>> ShowEnterConfirmDialog(string msg, string defaultInput)
            => default(Task<Tuple<SixMessageBoxResult, string>>);

        public Task<SixMessageBoxResult> MessageBox(MessageBoxDialogParams dialogParams) {
            Contract.Requires<ArgumentNullException>(dialogParams != null);
            return default(Task<SixMessageBoxResult>);
        }

        public Task ShowPopup(object vm, IDictionary<string, object> overrideSettings = null) {
            Contract.Requires<ArgumentNullException>(vm != null);
            return default(Task);
        }

        public Task ShowWindow(object vm, IDictionary<string, object> overrideSettings = null) {
            Contract.Requires<ArgumentNullException>(vm != null);
            return default(Task);
        }

        public Task<bool> ExceptionDialog(Exception e, string message, string title = null, object window = null) {
            Contract.Requires<ArgumentNullException>(e != null);
            return default(Task<bool>);
        }
    }

    [ContractClassFor(typeof (IDialogManager))]
    public abstract class DialogManagerContract : IDialogManager
    {
        public Task<string> BrowseForFolder(string selectedPath = null, string title = null) => default(Task<string>);

        public Task<string> BrowseForFile(string initialDirectory = null, string title = null,
            string defaultExt = null,
            bool checkFileExists = true) => default(Task<string>);

        /*
        public SixMessageBoxResult MessageBoxSync(MessageBoxDialogParams dialogParams) {
            Contract.Requires<ArgumentNullException>(dialogParams != null);
            return default(SixMessageBoxResult);
        }
*/

        public Task<SixMessageBoxResult> MessageBox(MessageBoxDialogParams dialogParams) {
            Contract.Requires<ArgumentNullException>(dialogParams != null);
            return default(Task<SixMessageBoxResult>);
        }

        public Task<bool> ExceptionDialog(Exception e, string message, string title = null, object owner = null) {
            Contract.Requires<ArgumentNullException>(e != null);
            Contract.Requires<ArgumentNullException>(message != null);
            return default(Task<bool>);
        }
    }
}