// <copyright company="SIX Networks GmbH" file="INodeApi.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using ReactiveUI;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Mini.Applications;

namespace SN.withSIX.Mini.Presentation.Electron
{
    public interface INodeApi
    {
        string Version { get; }
        ArgsO Args { get; }
        Task<RecoveryOptionResult> HandleUserError(UserError error);
        Task<string> ShowMessageBox(string title, string message, string[] buttons, string type = null);
        Task<string> ShowSaveDialog(string title = null, string defaultPath = null);
        Task<string[]> ShowFileDialog(string title = null, string defaultPath = null);
        Task<string[]> ShowFolderDialog(string title = null, string defaultPath = null);
        Task<IAbsoluteFilePath> DownloadFile(Uri url, string path, CancellationToken token);
        Task DownloadSession(Uri url, string path, CancellationToken token);
        Task<bool?> ShowNotification(string title, string message = null);
        Task DisplayTrayBaloon(string title, string content, string icon = null);
        Task SetState(BusyState state, string description, double? progress);
        Task InstallSelfUpdate();
        Task Exit(int exitCode);
    }

    public enum BusyState
    {
        Off,
        On
    }
}