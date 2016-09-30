// <copyright company="SIX Networks GmbH" file="ModsHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Play.Infra.Server.UseCases;

namespace withSIX.Play.Infra.Server.Hubs
{

    public class ModsHub : BaseHub
    {
        public ModsHub(IMediator mediator) : base(mediator) {}

        public async Task<IModInfo> GetModInfoQuery(Guid modId, Guid? gameId) => await
        _mediator.SendAsync(gameId != null
            ? new RequestModInformation(modId, gameId.Value)
            : new RequestModInformation(modId, gameId.Value)).ConfigureAwait(false);

        public async Task<Dictionary<Guid, IModInfo>> GetInstalledModInfosQueryByGame(Guid gameId) => await _mediator.SendAsync(new RequestModInformationByGame(gameId, true)).ConfigureAwait(false);
    }
}