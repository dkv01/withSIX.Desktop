// <copyright company="SIX Networks GmbH" file="ContentEngineGameContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.ContentEngine.Core;
using withSIX.ContentEngine.Infra.Services;
using withSIX.Core.Infra.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Infra.Data.Services
{
    public class ContentEngineGameContext : IContentEngineGameContext, IInfrastructureService
    {
        readonly IDbContextLocator _contextLocator;

        public ContentEngineGameContext(IDbContextLocator contextLocator) {
            _contextLocator = contextLocator;
        }

        public IContentEngineGame Get(Guid gameId) => _contextLocator.GetReadOnlyGameContext().FindGame(gameId);
    }
}