// <copyright company="SIX Networks GmbH" file="StartDownloadSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features
{
    public class StartDownloadSession : IAsyncVoidCommand, IExcludeGameWriteLock, IHaveId<Guid>
    {
        public StartDownloadSession(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    public class StartDownloadSessionHandler : DbCommandBase, IAsyncRequestHandler<StartDownloadSession>
    {
        private readonly IExternalFileDownloader _downloader;

        public StartDownloadSessionHandler(IExternalFileDownloader downloader, IDbContextLocator locator)
            : base(locator) {
            _downloader = downloader;
        }

        public async Task Handle(StartDownloadSession message) {
            var game = await GameContext.FindGameFromRequestOrThrowAsync(message).ConfigureAwait(false);
            await _downloader.StartSession(game.GetPublisherUrl(), game.GetContentPath()).ConfigureAwait(false);
            
        }
    }
}