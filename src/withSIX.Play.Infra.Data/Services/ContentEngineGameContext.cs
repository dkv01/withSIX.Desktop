// <copyright company="SIX Networks GmbH" file="ContentEngineGameContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.ContentEngine.Core;
using withSIX.ContentEngine.Infra.Services;
using withSIX.Core.Infra.Services;
using withSIX.Play.Applications.Services.Infrastructure;

namespace withSIX.Play.Infra.Data.Services
{
    public class ContentEngineGameContext : IContentEngineGameContext, IInfrastructureService
    {
        readonly IGameContext _gameContext;

        public ContentEngineGameContext(IGameContext gameContext) {
            _gameContext = gameContext;
        }

        public IContentEngineGame Get(Guid gameId) => _gameContext.Games.Find(gameId);
    }
}