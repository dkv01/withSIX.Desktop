// <copyright company="SIX Networks GmbH" file="GetGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using withSIX.Api.Models.Content.v3;

namespace SN.withSIX.Mini.Applications.Usecases.Main.Games
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