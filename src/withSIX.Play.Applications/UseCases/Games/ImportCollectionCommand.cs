// <copyright company="SIX Networks GmbH" file="ImportCollectionCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using MediatR;

namespace withSIX.Play.Applications.UseCases.Games
{
    public class ImportCollectionCommand : IAsyncRequest<Unit>, IRequireApiSession
    {
        public ImportCollectionCommand(Guid collectionId) {
            CollectionId = collectionId;
        }

        public Guid CollectionId { get; private set; }
    }
}