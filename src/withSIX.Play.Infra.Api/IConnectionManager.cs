// <copyright company="SIX Networks GmbH" file="IConnectionManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI;

using SN.withSIX.Play.Core.Connect.Infrastructure;
using SN.withSIX.Play.Infra.Api.Hubs;

namespace SN.withSIX.Play.Infra.Api
{

    interface IConnectionManager : IConnectionScoper
    {
        IMessageBus MessageBus { get; }
        ICollectionsHub CollectionsHub { get; }
        IMissionsHub MissionsHub { get; }
        bool IsConnected();
        bool IsLoggedIn();
    }
}