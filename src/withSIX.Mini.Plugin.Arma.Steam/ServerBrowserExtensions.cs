using System;
using System.Threading;
using System.Threading.Tasks;
using GameServerQuery;

namespace withSIX.Steam.Plugin.Arma
{
    public static class ServerBrowserExtensions
    {
        public static Task<IObservable<ArmaServerInfo>> GetServersInclDetails2(this ServerBrowser This, CancellationToken ct,
            ServerFilterBuilder filter, bool inclRules) {
            return This.GetServersInclDetails(ct, filter.GetServerFilterWrap(), inclRules);
        }

        public static Task<IObservable<ArmaServerInfo>> GetServers2(this ServerBrowser This, CancellationToken ct, ServerFilterBuilder filter) {
            return This.GetServers(ct, filter.GetServerFilterWrap());
        }
    }
}