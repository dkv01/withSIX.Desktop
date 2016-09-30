// <copyright company="SIX Networks GmbH" file="FetchCollectionQuery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using MediatR;
using withSIX.Api.Models.Collections;

namespace withSIX.Play.Applications.UseCases.Games
{
    public class FetchCollectionQuery : IAsyncRequest<CollectionModel>, IRequireApiSession
    {
        public FetchCollectionQuery(Guid collectionId) {
            CollectionId = collectionId;
        }

        public Guid CollectionId { get; }
    }
}