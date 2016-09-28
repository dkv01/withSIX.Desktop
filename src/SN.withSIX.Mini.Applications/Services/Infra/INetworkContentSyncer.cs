// <copyright company="SIX Networks GmbH" file="INetworkContentSyncer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Core.Games;
using withSIX.Api.Models.Content;

namespace SN.withSIX.Mini.Applications.Services.Infra
{
    public interface INetworkContentSyncer
    {
        Task SyncContent(IReadOnlyCollection<Game> games, ContentQuery query = null);

        Task SyncCollections(IReadOnlyCollection<SubscribedCollection> collections,
            bool countCheck = true);

        Task<IReadOnlyCollection<SubscribedCollection>> GetCollections(Guid gameId,
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