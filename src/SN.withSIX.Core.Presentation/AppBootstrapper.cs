// <copyright company="SIX Networks GmbH" file="AppBootstrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI;
using SimpleInjector;

namespace SN.withSIX.Core.Presentation
{
    public static class AppBootstrapper
    {
        public static void RegisterMessageBus(this Container container) {
            container.RegisterSingleton(new MessageBus());
            container.RegisterSingleton<IMessageBus>(container.GetInstance<MessageBus>);
        }
    }
}