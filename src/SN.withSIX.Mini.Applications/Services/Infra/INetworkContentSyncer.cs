// <copyright company="SIX Networks GmbH" file="INetworkContentSyncer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Applications.Services.Infra
{
    public interface INetworkContentSyncer
    {
        Task SyncContent(IReadOnlyCollection<Game> games);

        Task SyncCollections(IReadOnlyCollection<SubscribedCollection> collections,
            IReadOnlyCollection<NetworkContent> content, bool countCheck = true);

        Task<IReadOnlyCollection<SubscribedCollection>> GetCollections(Guid gameId,
            IReadOnlyCollection<Guid> collectionIds, IReadOnlyCollection<NetworkContent> content);
    }
}