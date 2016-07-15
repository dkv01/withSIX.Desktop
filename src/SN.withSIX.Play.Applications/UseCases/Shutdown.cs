// <copyright company="SIX Networks GmbH" file="Shutdown.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ShortBus;
using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Play.Applications.UseCases
{
    public class Shutdown : IVoidCommand {}

    public class ShutdownHandler : IVoidCommandHandler<Shutdown>
    {
        private readonly IShutdownHandler _shutdownHandler;

        public ShutdownHandler(IShutdownHandler shutdownHandler) {
            _shutdownHandler = shutdownHandler;
        }

        public UnitType Handle(Shutdown request) {
            _shutdownHandler.Shutdown();
            return UnitType.Default;
        }
    }
}