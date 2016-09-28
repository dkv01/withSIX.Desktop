// <copyright company="SIX Networks GmbH" file="GetGameCollections.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public abstract class GetContentBase : IHaveId<Guid>
    {
        protected GetContentBase(Guid id, int page) {
            Id = id;
            Page = page;
        }

        public int Page { get; }

        public Guid Id { get; }
    }

    public class GetGameCollections : GetContentBase, IAsyncQuery<CollectionsApiModel>
    {
        public GetGameCollections(Guid id, int page = 1) : base(id, page) {}
    }

    public class GetGameCollectionsHandler : DbQueryBase,
        IAsyncRequestHandler<GetGameCollections, CollectionsApiModel>
    {
        public GetGameCollectionsHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<CollectionsApiModel> Handle(GetGameCollections request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            return game.MapTo<CollectionsApiModel>(opt => opt.Items["ctx"] = new PagingContext {Page = request.Page});
        }
    }

    [DataContract]
    public class CollectionsApiModel : ContentsApiModel {}
}