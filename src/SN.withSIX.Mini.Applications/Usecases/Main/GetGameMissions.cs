// <copyright company="SIX Networks GmbH" file="GetGameMissions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class GetGameMissions : GetContentBase, IAsyncQuery<MissionsApiModel>
    {
        public GetGameMissions(Guid id, int page = 1) : base(id, page) { }
    }


    public class GetGameMissionsHandler : DbQueryBase, IAsyncRequestHandler<GetGameMissions, MissionsApiModel>
    {
        public GetGameMissionsHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<MissionsApiModel> Handle(GetGameMissions request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            return game.MapTo<MissionsApiModel>(opt => opt.Items["ctx"] = new PagingContext {Page = request.Page});
        }
    }

    [DataContract]
    public class MissionsApiModel : ContentsApiModel {}
}