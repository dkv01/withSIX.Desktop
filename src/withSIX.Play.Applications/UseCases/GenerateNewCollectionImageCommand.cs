// <copyright company="SIX Networks GmbH" file="GenerateNewCollectionImageCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Extensions;
using withSIX.Play.Core.Connect.Infrastructure;
using withSIX.Play.Core.Games.Legacy;

namespace withSIX.Play.Applications.UseCases
{
    public class GenerateNewCollectionImageCommand : IAsyncRequest<Unit>, IRequireApiSession
    {
        public GenerateNewCollectionImageCommand(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class GenerateNewCollectionImageCommandHandler :
        IAsyncRequestHandler<GenerateNewCollectionImageCommand, Unit>
    {
        readonly IConnectApiHandler _api;
        readonly IContentManager _contentList;

        public GenerateNewCollectionImageCommandHandler(IConnectApiHandler api, IContentManager contentList) {
            _api = api;
            _contentList = contentList;
        }

        public Task<Unit> Handle(GenerateNewCollectionImageCommand request) {
            var collection = _contentList.CustomCollections.First(x => x.Id == request.Id);
            return collection.GenerateNewAvatar(_api).Void();
        }
    }
}