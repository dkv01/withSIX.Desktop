// <copyright company="SIX Networks GmbH" file="ServerHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Core;
using withSIX.Mini.Infra.Api.Hubs;
using withSIX.Steam.Infra;
using withSIX.Steam.Presentation.Usecases;

namespace withSIX.Steam.Presentation.Hubs
{
    public class ServerHub : HubBase<IServerHubClient>
    {
        public Task<BatchResult> GetServers(GetServers query, Guid requestId) => Send(query, requestId);
        public Task<BatchResult> GetServerAddresses(GetServerAddresses query, Guid requestId) => Send(query, requestId);
    }

    public interface IServerHubClient
    {
        Task ServerPageReceived(ReceivedServerPageEvent serverPage, Guid requestId);
        Task ServerAddressesPageReceived(ReceivedServerAddressesPageEvent serverPage, Guid requestId);
    }
}