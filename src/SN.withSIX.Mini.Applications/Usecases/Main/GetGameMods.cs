// <copyright company="SIX Networks GmbH" file="GetGameMods.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services.Infra;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class GetGameMods : GetContentBase, IAsyncQuery<ModsApiModel>
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
    public class ModsApiModel : ContentsApiModel { }
}