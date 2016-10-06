using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Core.Infra.Services;
using withSIX.Steam.Core.Services;

namespace withSIX.Steam.Infra
{
    public interface ISteamServiceSession {
        Task Start(uint appId);
        Task<ServersInfo<T>> GetServers<T>(bool inclExtendedDetails, List<IPEndPoint> ipEndPoints);
    }

    // TODO: Use factory to create based on choice

    public abstract class SteamServiceSession : ISteamServiceSession
    {
        public abstract Task Start(uint appId);
        public abstract Task<ServersInfo<T>> GetServers<T>(bool inclExtendedDetails, List<IPEndPoint> ipEndPoints);
    }

    public class SteamServiceSessionHttp : SteamServiceSession, IInfrastructureService
    {
        public override Task Start(uint appId) {
            // TODO: Test the connection? 
            return TaskExt.Default;
        }

        public override async Task<ServersInfo<T>> GetServers<T>(bool inclExtendedDetails, List<IPEndPoint> ipEndPoints) {
            var r = await new {
                IncludeDetails = true,
                IncludeRules = inclExtendedDetails,
                Addresses = ipEndPoints
            }.PostJson<ServersInfo<T>>(new Uri("http://127.0.0.66:48667/api/get-server-info")).ConfigureAwait(false);
            return r;
        }
    }

    public class SteamServiceSessionSignalR : SteamServiceSession, IInfrastructureService
    {
        public override Task Start(uint appId) {
            // TODO: Open up the connection
            return TaskExt.Default;
        }

        public override async Task<ServersInfo<T>> GetServers<T>(bool inclExtendedDetails, List<IPEndPoint> ipEndPoints) {
            var r = await new {
                IncludeDetails = true,
                IncludeRules = inclExtendedDetails,
                Addresses = ipEndPoints
            }.PostJson<ServersInfo<T>>(new Uri("http://127.0.0.66:48667/api/get-server-info")).ConfigureAwait(false);
            return r;
        }
    }
}
