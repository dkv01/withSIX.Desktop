// <copyright company="SIX Networks GmbH" file="CoreCheat.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Services;
using withSIX.Core.Services.Infrastructure;

namespace withSIX.Mini.Core
{
    public static class CoreCheat
    {
        static ICoreCheatImpl _impl;
        public static IDomainEventHandlerGrabber EventGrabber => _impl.EventGrabber;

        public static void SetServices(ICoreCheatImpl impl) {
            _impl = impl;
        }
    }

    public interface ICoreCheatImpl
    {
        IDomainEventHandlerGrabber EventGrabber { get; }
    }

    public class CoreCheatImpl : ICoreCheatImpl, IDomainService
    {
        public CoreCheatImpl(IDomainEventHandlerGrabber eventGrabber) {
            EventGrabber = eventGrabber;
        }

        public IDomainEventHandlerGrabber EventGrabber { get; }
    }
}