// <copyright company="SIX Networks GmbH" file="Shutdown.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace SN.withSIX.Mini.Applications.Usecases
{
    public class Shutdown : IAsyncVoidCommand {}

    public class ShutdownCommandHandler : IAsyncVoidCommandHandler<Shutdown>
    {
        readonly IContentInstallationService _contentInstallation;
        readonly IShutdownHandler _shutdownHandler;

        public ShutdownCommandHandler(IShutdownHandler shutdownHandler, IContentInstallationService contentInstallation) {
            _shutdownHandler = shutdownHandler;
            _contentInstallation = contentInstallation;
        }

        public async Task<UnitType> HandleAsync(Shutdown request) {
            await _contentInstallation.Abort().ConfigureAwait(false);
            _shutdownHandler.Shutdown();

            return UnitType.Default;
        }
    }
}