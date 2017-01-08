// <copyright company="SIX Networks GmbH" file="Shutdown.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace withSIX.Mini.Applications.Features
{
    // We don't want a DB scope+save
    public class Shutdown : IAsyncVoidCommand, IExcludeGameWriteLock {}

    public class ShutdownCommandHandler : IAsyncVoidCommandHandler<Shutdown>
    {
        readonly IContentInstallationService _contentInstallation;
        readonly IShutdownHandler _shutdownHandler;

        public ShutdownCommandHandler(IShutdownHandler shutdownHandler, IContentInstallationService contentInstallation) {
            _shutdownHandler = shutdownHandler;
            _contentInstallation = contentInstallation;
        }

        public async Task<Unit> Handle(Shutdown request) {
            await _contentInstallation.Abort().ConfigureAwait(false);
            _shutdownHandler.Shutdown();

            return Unit.Value;
        }
    }
}