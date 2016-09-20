// <copyright company="SIX Networks GmbH" file="SetupGameStuff.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;

namespace SN.withSIX.Mini.Applications
{
    public interface ISetupGameStuff
    {
        Task Initialize();

        Task HandleGameContentsWhenNeeded(IReadOnlyCollection<Guid> gameIds, ContentQuery query = null);
    }

    public static class GameStuffExtensions
    {
        public static Task HandleGameContentsWhenNeeded(this ISetupGameStuff This,
            ContentQuery query = null, params Guid[] gameIds)
            => This.HandleGameContentsWhenNeeded(gameIds, query);
    }
}