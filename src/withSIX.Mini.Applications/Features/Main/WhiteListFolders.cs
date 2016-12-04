// <copyright company="SIX Networks GmbH" file="WhiteListFolders.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    public class WhiteListFolders : IAsyncVoidCommand
    {
        public WhiteListFolders(List<string> folders) {
            Folders = folders;
        }

        public List<string> Folders { get; }
    }


    public class WhiteListFoldersHandler : ApiDbCommandBase, IAsyncVoidCommandHandler<WhiteListFolders>
    {
        private readonly IFolderHandler _folderHandler;

        public WhiteListFoldersHandler(IDbContextLocator dbContextLocator, IFolderHandler folderHandler)
            : base(dbContextLocator) {
            _folderHandler = folderHandler;
        }

        public Task<Unit> Handle(WhiteListFolders request) {
            foreach (var f in request.Folders.Select(x => x.ToAbsoluteDirectoryPath()))
                _folderHandler.WhiteListFolder(f);
            //await Store(request);
            return Task.FromResult(Unit.Value);
        }

        /*        private async Task Store(WhiteListFolders request) {
                    var c = GameLinkContext.Context;

                    foreach (
                        var f in
                            request.Folders.Select(x => x.ToAbsoluteDirectoryPath()).Where(x => !c.Infos.Any(i => x.Equals(i.Path))))
                        c.Infos.Add(new FolderInfo(f, new ContentInfo()));

                    await ContentLinkContext.Save().ConfigureAwait(false);
                }*/
    }
}