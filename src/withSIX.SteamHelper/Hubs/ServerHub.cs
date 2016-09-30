﻿// <copyright company="SIX Networks GmbH" file="ServerHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Steam.Presentation.Usecases;

namespace withSIX.Steam.Presentation.Hubs
{
    public class ServerHub : HubBase<IServerHubClient>
    {
        public Task<ServerInfo> GetServerInfo(GetServerInfo query, Guid requestId) => SendAsync(query, requestId);
    }

    public interface IServerHubClient {}
}