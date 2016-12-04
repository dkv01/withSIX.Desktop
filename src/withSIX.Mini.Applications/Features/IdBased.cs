// <copyright company="SIX Networks GmbH" file="IdBased.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Api.Models.Content.v3;

namespace withSIX.Mini.Applications.Features
{
    public abstract class IdBased : GameContentBase, IHaveId<Guid>
    {
        protected IdBased(Guid gameId, Guid id) : base(gameId) {
            Id = id;
        }

        public Guid Id { get; }
    }
}