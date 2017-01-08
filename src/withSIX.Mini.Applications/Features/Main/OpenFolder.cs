// <copyright company="SIX Networks GmbH" file="OpenFolder.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Features.Main
{
    public class OpenFolder : IAsyncVoidCommand, IExcludeGameWriteLock, IHaveGameId
    {
        public OpenFolder(Guid gameId, Guid? id = null, FolderType folderType = FolderType.Default) {
            GameId = gameId;
            Id = id;
            FolderType = folderType;
        }

        public Guid? Id { get; }

        public FolderType FolderType { get; }

        public Guid GameId { get; }
    }

    public enum FolderType
    {
        Default,
        Config
    }

    public class OpenFolderHandler : ApiDbCommandBase, IAsyncRequestHandler<OpenFolder>
    {
        public OpenFolderHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task Handle(OpenFolder request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            var path = GetPath(game, request.Id, request.FolderType);

            Tools.FileUtil.OpenFolderInExplorer(path.GetNearestExisting());

            
        }

        private static IAbsoluteDirectoryPath GetPath(Game game, Guid? id, FolderType type) {
            if (!id.HasValue)
                return game.GetPath();

            if (id.Value == Guid.Empty) {
                return type == FolderType.Config
                    ? game.GetConfigPath()
                    : game.GetContentPath();
            }
            var content = game.Contents.OfType<IPackagedContent>().FindOrThrow(id.Value);
            return type == FolderType.Config
                ? game.GetConfigPath(content)
                : game.GetContentPath(content);
        }
    }
}