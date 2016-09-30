// <copyright company="SIX Networks GmbH" file="UploadNewCollectionImageCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using withSIX.Play.Core.Connect.Infrastructure;
using withSIX.Play.Core.Games.Legacy;

namespace withSIX.Play.Applications.UseCases
{
    public class UploadNewCollectionImageCommand : IAsyncRequest<Unit>, IRequireApiSession
    {
        public UploadNewCollectionImageCommand(Guid id, IAbsoluteFilePath filePath) {
            Id = id;
            FilePath = filePath;
        }

        public Guid Id { get; }
        public IAbsoluteFilePath FilePath { get; }
    }

    public class UploadNewCollectionImageCommandHandler :
        IAsyncRequestHandler<UploadNewCollectionImageCommand, Unit>
    {
        readonly IConnectApiHandler _api;
        readonly IContentManager _contentList;

        public UploadNewCollectionImageCommandHandler(IConnectApiHandler api, IContentManager contentList) {
            _api = api;
            _contentList = contentList;
        }

        public Task<Unit> Handle(UploadNewCollectionImageCommand request) {
            var size = request.FilePath.FileInfo.Length;
            SizeExtensions.DefaultContentLengthRange.VerifySize(size);
            var collection = _contentList.CustomCollections
                .First(x => x.Id == request.Id);
            return collection.UploadAvatar(request.FilePath, _api).Void();
        }
    }
}