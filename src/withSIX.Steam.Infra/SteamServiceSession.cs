// <copyright company="SIX Networks GmbH" file="SteamServiceSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Steam.Core.Services;

namespace withSIX.Steam.Infra
{
    public interface ISteamServiceSession
    {
        Task Start(uint appId, Uri uri);
        Task<ServersInfo<T>> GetServers<T>(bool inclExtendedDetails, List<IPEndPoint> ipEndPoints);
    }

    public abstract class SteamServiceSession : ISteamServiceSession
    {
        public abstract Task Start(uint appId, Uri uri);
        public abstract Task<ServersInfo<T>> GetServers<T>(bool inclExtendedDetails, List<IPEndPoint> ipEndPoints);
    }

    public class SteamServiceSessionHttp : SteamServiceSession
    {
        private Uri _uri;

        public override Task Start(uint appId, Uri uri) {
            _uri = uri;
            // TODO: Test the connection? 
            return TaskExt.Default;
        }

        public override async Task<ServersInfo<T>> GetServers<T>(bool inclExtendedDetails, List<IPEndPoint> ipEndPoints) {
            var r = await new {
                IncludeDetails = true,
                IncludeRules = inclExtendedDetails,
                Addresses = ipEndPoints
            }.PostJson<ServersInfo<T>>(new Uri(_uri, "/api/get-server-info")).ConfigureAwait(false);
            return r;
        }
    }

    public class SteamServiceSessionSignalR : SteamServiceSession
    {
        private HubConnection _con;
        private IHubProxy _servers;

        public override Task Start(uint appId, Uri uri) {
            _con?.Stop();
            _con?.Dispose();
            var con = new HubConnection(new Uri(uri, "/signalr").ToString());
            _servers = con.CreateHubProxy("ServerHub");
            _con = con;
            return con.Start();
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