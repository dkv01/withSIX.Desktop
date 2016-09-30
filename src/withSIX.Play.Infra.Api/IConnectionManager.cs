// <copyright company="SIX Networks GmbH" file="IConnectionManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI;
using withSIX.Play.Core.Connect.Infrastructure;
using withSIX.Play.Infra.Api.Hubs;

namespace withSIX.Play.Infra.Api
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