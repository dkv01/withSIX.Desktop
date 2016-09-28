// <copyright company="SIX Networks GmbH" file="PrepareFolder.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Attributes;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    [ApiUserAction]
    public class PrepareFolder : IAsyncCommand<string> {}

    public class PrepareFolderHandler : DbCommandBase, IAsyncRequestHandler<PrepareFolder, string>
    {
        readonly IDialogManager _dialogManager;
        readonly IFolderHandler _folderHandler;

        public PrepareFolderHandler(IDbContextLocator dbContextLocator, IFolderHandler folderHandler,
            IDialogManager dialogManager) : base(dbContextLocator) {
            _folderHandler = folderHandler;
            _dialogManager = dialogManager;
        }

        public async Task<string> Handle(PrepareFolder request) {
            // open dialog, ask for folder
            var folder =
                await
                    _dialogManager.BrowseForFolder(title: "Select folder to upload to the withSIX network")
                        .ConfigureAwait(false);
            if (folder == null)
                throw new OperationCanceledException("The user cancelled the operation");
            // TODO: Restructure suggestions etc?
            _folderHandler.WhiteListFolder(folder.ToAbsoluteDirectoryPath());
            return folder;
        }
    }
}