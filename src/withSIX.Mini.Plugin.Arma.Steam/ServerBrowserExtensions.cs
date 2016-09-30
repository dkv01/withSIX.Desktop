using System;
using System.Threading;
using System.Threading.Tasks;

namespace withSIX.Steam.Plugin.Arma
{
    public static class ServerBrowserExtensions
    {
        public static Task<IObservable<ArmaServerInfo>> GetServersInclDetails2(this ServerBrowser This, CancellationToken ct,
            ServerFilterBuilder filter, bool inclRules) {
            return This.GetServersInclDetails(ct, filter.Value, inclRules);
        }

        public static Task<IObservable<ArmaServerInfo>> GetServers2(this ServerBrowser This, CancellationToken ct, ServerFilterBuilder filter) {
            return This.GetServers(ct, filter.Value);
        }
    }
}