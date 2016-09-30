// <copyright company="SIX Networks GmbH" file="UnsubscribeFromCollectionCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using MediatR;

namespace withSIX.Play.Applications.UseCases.Games
{
    public class UnsubscribeFromCollectionCommand : IAsyncRequest<Unit>, IRequireApiSession
    {
        public UnsubscribeFromCollectionCommand(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }
}