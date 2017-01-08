// <copyright company="SIX Networks GmbH" file="GetUploadFolder.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    [ApiUserAction]
    public class GetUploadFolder : IQuery<IAbsoluteDirectoryPath>
    {
        public GetUploadFolder(Guid contentId) {
            ContentId = contentId;
        }

        public Guid ContentId { get; }
    }

    public class GetUploadFolderHandler : ApiDbQueryBase, IAsyncRequestHandler<GetUploadFolder, IAbsoluteDirectoryPath>
    {
        public GetUploadFolderHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<IAbsoluteDirectoryPath> Handle(GetUploadFolder request)
            =>
                (await ContentLinkContext.GetFolderLink().ConfigureAwait(false)).Infos.FirstOrDefault(
                    x => x.ContentInfo.ContentId == request.ContentId)?
                .Path;
    }
}