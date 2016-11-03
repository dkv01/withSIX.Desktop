// <copyright company="SIX Networks GmbH" file="GetGameMissions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Usecases.Main
{
    public class GetGameMissions : GetContentBase, IAsyncQuery<MissionsApiModel>
    {
        public GetGameMissions(Guid id, int page = 1) : base(id, page) {}
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
    public class MissionsApiModel : ContentsApiModel
    {
        public MissionsApiModel(List<ContentApiModel> items, int total, int pageNumber, int pageSize)
            : base(items, total, pageNumber, pageSize) {}
    }
}