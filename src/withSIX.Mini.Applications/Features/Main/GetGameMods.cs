// <copyright company="SIX Networks GmbH" file="GetGameMods.cs">
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

namespace withSIX.Mini.Applications.Features.Main
{
    public class GetGameMods : GetContentBase, IQuery<ModsApiModel>
    {
        public GetGameMods(Guid id, int page = 1) : base(id, page) {}
    }

    public class GetGameModsHandler : DbQueryBase, IAsyncRequestHandler<GetGameMods, ModsApiModel>
    {
        public GetGameModsHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<ModsApiModel> Handle(GetGameMods request) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(request).ConfigureAwait(false);
            return game.MapTo<ModsApiModel>(opt => opt.Items["ctx"] = new PagingContext {Page = request.Page});
        }
    }


    [DataContract]
    public class ModsApiModel : ContentsApiModel
    {
        public ModsApiModel(List<ContentApiModel> items, int total, int pageNumber, int pageSize)
            : base(items, total, pageNumber, pageSize) {}
    }
}