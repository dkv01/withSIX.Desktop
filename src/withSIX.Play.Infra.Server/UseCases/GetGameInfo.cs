// <copyright company="SIX Networks GmbH" file="GetGameInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using MediatR;
using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Infra.Server.UseCases
{
    public class GetGameInfoQuery : IRequest<List<GameInfo>> {}

    public class GetGameInfoQueryHandler : IRequestHandler<GetGameInfoQuery, List<GameInfo>>
    {
        readonly IGameContext _gameContext;

        public GetGameInfoQueryHandler(IGameContext gameContext) {
            _gameContext = gameContext;
        }

        public List<GameInfo> Handle(GetGameInfoQuery request) => _gameContext.Games.Select(BuildGameInfo).ToList();

        GameInfo BuildGameInfo(Game game) => new GameInfo {
            Id = game.Id,
            Name = game.MetaData.Name,
            Collections = BuildCollectionList(game)
        };

        List<CollectionInfo> BuildCollectionList(Game game) {
            //mm
            lock (_gameContext.CustomCollections.Local)
                return
                    Enumerable.ToList(_gameContext.CustomCollections.Where(x => x.GameMatch(game))
                        .Select(Mapper.Map<CollectionInfo>));
        }
    }

    public class GameInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<CollectionInfo> Collections { get; set; }
    }

    public class CollectionInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ShortId { get; set; }
    }
}