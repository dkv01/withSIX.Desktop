// <copyright company="SIX Networks GmbH" file="BrowseFolder.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    [ApiUserAction]
    public class BrowseFolder : IAsyncQuery<string>
    {
        public BrowseFolder(FolderDialogOptions options) {
            Options = options;
        }

        public FolderDialogOptions Options { get; }
    }

    public class FolderDialogOptions
    {
        public string DefaultPath { get; set; }
    }


    public class BrowseFolderHandler : DbQueryBase, IAsyncRequestHandler<BrowseFolder, string>
    {
        readonly IDialogManager _dialogManager;

        public BrowseFolderHandler(IDbContextLocator dbContextLocator, IDialogManager dialogManager)
            : base(dbContextLocator) {
            _dialogManager = dialogManager;
        }

        public async Task<string> Handle(BrowseFolder request) {
            // open dialog, ask for folder
            var folder =
                await
                    _dialogManager.BrowseForFolder(request.Options.DefaultPath, "Select folder for setting")
                        .ConfigureAwait(false);
            if (folder == null)
                throw new OperationCanceledException("The user cancelled the operation");
            return folder;
        }
    }
}