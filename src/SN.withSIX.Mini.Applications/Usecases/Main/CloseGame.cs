// <copyright company="SIX Networks GmbH" file="CloseGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Attributes;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    [ApiUserAction("Close")]
    public class CloseGame : RequestBase, IHaveId<Guid>, IAsyncVoidCommand
    {
        public CloseGame(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class CloseGameHandler : ApiDbCommandBase, IAsyncVoidCommandHandler<CloseGame>
    {
        public CloseGameHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<UnitType> HandleAsync(CloseGame request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            game.Close();
            return UnitType.Default;
        }
    }
}