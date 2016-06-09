﻿// <copyright company="SIX Networks GmbH" file="OpenFolder.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class OpenFolder : IAsyncVoidCommand, IExcludeGameWriteLock, IHaveGameId
    {
        public OpenFolder(Guid gameId, Guid? id = null) {
            GameId = gameId;
            Id = id;
        }

        public Guid? Id { get; }

        public Guid GameId { get; }
    }

    public class OpenFolderHandler : ApiDbCommandBase, IAsyncVoidCommandHandler<OpenFolder>
    {
        public OpenFolderHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<UnitType> HandleAsync(OpenFolder request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);

            if (request.Id.HasValue) {
                if (request.Id.Value == Guid.Empty)
                    Tools.FileUtil.OpenFolderInExplorer(game.GetContentPath());
                else {
                    var content = game.Contents.OfType<IPackagedContent>().FindOrThrow(request.Id.Value);
                    Tools.FileUtil.OpenFolderInExplorer(game.GetContentPath(content));
                }
            } else
                Tools.FileUtil.OpenFolderInExplorer(game.GetPath());

            return UnitType.Default;
        }
    }
}