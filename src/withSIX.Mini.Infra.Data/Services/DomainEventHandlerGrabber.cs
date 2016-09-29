// <copyright company="SIX Networks GmbH" file="DomainEventHandlerGrabber.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Infra.Services;
using withSIX.Core.Services.Infrastructure;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Infra.Data.Services
{
    public class DomainEventHandlerGrabber : IDomainEventHandlerGrabber, IInfrastructureService
    {
        readonly IDbContextLocator _contextFactory;

        public DomainEventHandlerGrabber(IDbContextLocator contextFactory) {
            _contextFactory = contextFactory;
        }

        public IDomainEventHandler Get() {
            var context = _contextFactory.GetGameContext();
            return context.DomainEventHandler;
        }

        public IDomainEventHandler GetSettings() {
            var context = _contextFactory.GetSettingsContext();
            return context.DomainEventHandler;
        }
    }
}