// <copyright company="SIX Networks GmbH" file="PingProvider.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using withSIX.Core.Logging;
using withSIX.Core.Services.Infrastructure;

namespace withSIX.Core.Infra.Services
{
    public class PingProvider : IPingProvider, IEnableLogging, IInfrastructureService
    {
        const int DefaultTimeout = 3000;

        [Obsolete("use async")]
        public long Ping(string hostName, int count = 3) {
            using (var p = new Ping()) {
                var pings = new List<long>();
                var i = 0;
                while (i < count) {
                    i++;
                    if (!TryPing(hostName, p, pings))
                        break;
                }

                return pings.Any() ? (long) pings.Average() : Common.MagicPingValue;
            }
        }

        public async Task<long> PingAsync(string hostName, int count = 3) {
            using (var p = new Ping()) {
                var pings = new List<long>();
                var i = 0;
                while (i < count) {
                    i++;
                    if (!await TryPingAsync(hostName, p, pings).ConfigureAwait(false))
                        break;
                }

                return pings.Any() ? (long) pings.Average() : Common.MagicPingValue;
            }
        }

        bool TryPing(string hostName, Ping p, ICollection<long> pings) => TryPingAsync(hostName, p, pings).Result;

        async Task<bool> TryPingAsync(string hostName, Ping p, ICollection<long> pings) {
            try {
                var reply = await p.SendPingAsync(hostName, DefaultTimeout).ConfigureAwait(false);
                if ((reply != null) && (reply.Status == IPStatus.Success))
                    pings.Add(reply.RoundtripTime);
                return true;
            } catch (Exception e) {
                this.Logger().FormattedWarnException(e);
                return false;
            }
        }
    }
}