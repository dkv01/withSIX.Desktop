// <copyright company="SIX Networks GmbH" file="SteamServiceSessionSignalR.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;
using withSIX.Steam.Core.Services;

namespace withSIX.Steam.Infra
{
    public class SteamServiceSessionSignalR : SteamServiceSession
    {
        private HubConnection _con;
        private IHubProxy _servers;

        public override Task Start(uint appId, Uri uri) {
            _con?.Stop();
            _con?.Dispose();
            var con = new HubConnection(new Uri(uri, "/signalr").ToString()) {
                JsonSerializer = JsonSerializer.Create(JsonSupport.DefaultSettings)
            };
            _servers = con.CreateHubProxy("ServerHub");
            _con = con;
            return con.Start(new DefaultHttpClient());
        }

        public override async Task<ServersInfo<T>> GetServers<T>(bool inclExtendedDetails, List<IPEndPoint> ipEndPoints) {
            var r = await _servers.Invoke<ServersInfo<T>>("GetServerInfo", new {
                IncludeDetails = true,
                IncludeRules = inclExtendedDetails,
                Addresses = ipEndPoints
            }, Guid.NewGuid()).ConfigureAwait(false);
            return r;
        }
    }
}