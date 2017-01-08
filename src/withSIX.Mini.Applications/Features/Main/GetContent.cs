// <copyright company="SIX Networks GmbH" file="GetContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    public class GetContent : IQuery<ClientContentInfo2>, IHaveId<Guid>
    {
        public GetContent(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class GetContentHandler : ApiDbQueryBase, IAsyncRequestHandler<GetContent, ClientContentInfo2>
    {
        public GetContentHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<ClientContentInfo2> Handle(GetContent request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            return game.MapTo<ClientContentInfo2>();
        }
    }

    public class ClientContentInfo2
    {
        public List<RecentContentModel> RecentContent { get; set; }
        public List<InstalledContentModel> InstalledContent { get; set; }
        public List<LocalCollectionModel> LocalCollections { get; set; }
    }

    public abstract class ContentModel
    {
        public Guid Id { get; set; }
        public string PackageName { get; set; }
        public string Name { get; set; }
        public Guid GameId { get; set; }
        public Guid? OriginalGameId { get; set; }
        public string OriginalGameSlug { get; set; }
        // Is Only for Collections atm..
        public bool HasServers { get; set; }
        // Should be ContentsCount ? Is also mostly for Collections..
        public int ModsCount { get; set; }
    }

    public class InstalledContentModel : ContentModel
    {
        // TODO: What is this?
        public Guid ContentId { get; set; }
    }

    public class RecentContentModel : ContentModel {}

    public class FavoriteContentModel : ContentModel {}

    public class LocalCollectionModel : ContentModel {}
}