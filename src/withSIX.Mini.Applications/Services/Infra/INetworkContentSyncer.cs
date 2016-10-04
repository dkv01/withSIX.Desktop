// <copyright company="SIX Networks GmbH" file="INetworkContentSyncer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Services.Infra
{
    public interface INetworkContentSyncer
    {
        Task SyncContent(IReadOnlyCollection<Game> games, ContentQuery query = null);

        Task SyncCollections(IReadOnlyCollection<Mini.Core.Games.SubscribedCollection> collections,
            bool countCheck = true);

        Task<IReadOnlyCollection<Mini.Core.Games.SubscribedCollection>> GetCollections(Guid gameId,
            IReadOnlyCollection<Guid> collectionIds);
    }

    public class ContentQuery
    {
        public List<ContentPublisherApiJson> Publishers { get; set; } = new List<ContentPublisherApiJson>();
        public List<string> PackageNames { get; set; } = new List<string>();
        public List<Guid> Ids { get; set; } = new List<Guid>();

        public bool IsMatch(ModClientApiJsonV3WithGameId c)
            => c.Publishers.Any(x => Publishers.Any(p => (p.Id == x.Id) && (p.Type == x.Type)))
               || PackageNames.ContainsIgnoreCase(c.PackageName)
               || Ids.Contains(c.Id);

        public bool IsMatch(ModNetworkContent c)
            => c.Publishers.Any(x => Publishers.Any(p => (p.Id == x.PublisherId) && (p.Type == x.Publisher)))
               || PackageNames.ContainsIgnoreCase(c.PackageName)
               || Ids.Contains(c.Id);
    }
}