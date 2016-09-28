// <copyright company="SIX Networks GmbH" file="GetFolders.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class GetFolders : IAsyncQuery<List<FolderInfo>>
    {
        public GetFolders(List<string> folders) {
            Folders = folders;
        }

        public List<string> Folders { get; set; }
    }

    public class GetFoldersHandler : ApiDbQueryBase, IAsyncRequestHandler<GetFolders, List<FolderInfo>>
    {
        public GetFoldersHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<List<FolderInfo>> Handle(GetFolders request)
            =>
            (await ContentLinkContext.GetFolderLink().ConfigureAwait(false)).Infos.Where(
                x => request.Folders.Contains(x.Path.ToString())).ToList();
    }
}