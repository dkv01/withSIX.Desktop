// <copyright company="SIX Networks GmbH" file="GetGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Usecases.Main.Games
{
    [Obsolete]
    public class GetGame : IAsyncVoidCommand, IHaveId<Guid>
    {
        public GetGame(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class GetGameHandler : DbQueryBase, IAsyncRequestHandler<GetGame, Unit>
    {
        public GetGameHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<Unit> Handle(GetGame request) {
            return Unit.Value;
        }
    }
}