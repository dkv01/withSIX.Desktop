// <copyright company="SIX Networks GmbH" file="HostPicker.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;
using withSIX.Sync.Core.Transfer;
using withSIX.Sync.Core.Transfer.MirrorSelectors;

namespace withSIX.Sync.Core.Legacy
{
    public class AllZsyncFailException : Exception
    {
        public AllZsyncFailException(string message) : base(message) {}
    }


    public enum HostType
    {
        Unknown,
        Rsync,
        Zsync,
        Http,
        Ftp,
        Ssh,
        Local
    }

    public class ExportLifetimeContext<T> : IDisposable
    {
        private readonly Action _dispose;

        public ExportLifetimeContext(T value, Action dispose) {
            Value = value;
            _dispose = dispose;
        }

        public T Value { get; }

        public void Dispose() {
            _dispose();
        }
    }

    public class HostPicker : IEnableLogging
    {
        readonly IHostChecker _hostChecker;

        public HostPicker(IEnumerable<Uri> hosts, MultiThreadingSettings multiThreadingSettings,
            Func<ExportLifetimeContext<IHostChecker>> hostChecker) {
            if (hosts == null) throw new ArgumentNullException(nameof(hosts));

            ZsyncIncompatHosts = new List<Uri>();
            MultiThreadingSettings = multiThreadingSettings;

            _hostChecker = hostChecker().Value;

            foreach (var host in hosts)
                HostStates[host] = 0;
        }

        public Dictionary<Uri, int> HostStates { get; } = new Dictionary<Uri, int>();

        public MultiThreadingSettings MultiThreadingSettings { get; }
        public List<Uri> ZsyncIncompatHosts { get; }
        ProtocolPreference ProtocolPreference { get; }

        HostType GetHostType(Uri host) => _hostChecker.GetHostType(host);

        public virtual void ZsyncIncompatHost(Uri host) {
            lock (ZsyncIncompatHosts) {
                if (!ZsyncIncompatHosts.Contains(host))
                    ZsyncIncompatHosts.Add(host);
            }
        }

        public virtual Uri Pick(Uri previousHost = null, bool filterHostname = true) {
            if (!MultiThreadingSettings.MultiMirror)
                return PickHost(previousHost);

            var c = MultiThreadingSettings.MaxThreads > 2
                ? (int) Math.Round((decimal) MultiThreadingSettings.MaxThreads/MultiThreadingSettings.HostThreadDiv)
                : MultiThreadingSettings.MaxThreads;
            var hosts = PickHosts(MultiThreadingSettings.MaxThreads > 1 ? c : 1, filterHostname, previousHost);

            var random = new Random();
            return hosts[random.Next(hosts.Length)];
        }

        public virtual Uri PickHost(Uri previousHost = null) {
            lock (HostStates) {
                if (previousHost != null)
                    MarkBad(previousHost);

                var validHosts = GetValidHosts();
                if (!validHosts.Any())
                    throw new HostListExhausted("No more hosts to pick");
                var host = validHosts.First();
                this.Logger().Info("Selected host: {0}", host);
                return host;
            }
        }

        Uri[] GetValidHosts() => SortHosts(HostStates.Where(x => (x.Value < 3) && _hostChecker.ValidateHost(x.Key)))
            .Select(x => x.Key)
            .ToArray();

        public void MarkBad(Uri previousHost) {
            lock (HostStates) {
                this.Logger().Info("Marking {0} as bad ({1})", previousHost, HostStates[previousHost] + 1);
                HostStates[previousHost]++;
            }
        }

        Uri[] PickHosts(int count = 1, bool filterHostname = true, Uri previousHost = null) {
            var hosts = new List<Uri>();
            var hostNames = new List<string>();

            lock (HostStates) {
                if (previousHost != null)
                    MarkBad(previousHost);

                var hostKeys = GetValidHosts();
                if (hostKeys.Length < count)
                    filterHostname = false;

                foreach (var host in hostKeys) {
                    if (hosts.Count >= count)
                        break;

                    if (host.Scheme == "file")
                        hosts.Add(host);
                    else {
                        var hostName = host.Host;
                        if (string.IsNullOrWhiteSpace(hostName)) {
                            this.Logger().Warn("Empty hostname for {0}", host);
                            continue;
                        }

                        if (!filterHostname || hostNames.None(x => x == hostName)) {
                            if (filterHostname)
                                hostNames.Add(hostName);
                            hosts.Add(host);
                        }
                    }
                }

                var left = count - hosts.Count;
                if (hosts.Any() && (left > 0)) {
                    while (left > 0) {
                        hosts.Add(hosts[0]);
                        left--;
                    }
                }
            }

            if (!hosts.Any())
                throw new HostListExhausted("No more hosts to pick");

            return hosts.ToArray();
        }

        IEnumerable<KeyValuePair<Uri, int>> SortHosts(IEnumerable<KeyValuePair<Uri, int>> hosts) {
            if (hosts == null) throw new ArgumentNullException(nameof(hosts));

            switch (ProtocolPreference) {
            case ProtocolPreference.PreferRsync:
                return hosts
                    .OrderBy(
                        x =>
                            (x.Value == 0) && (GetHostType(x.Key) == HostType.Rsync)
                                ? 0
                                : 1)
                    .ToArray();
            case ProtocolPreference.PreferZsync:
                return hosts
                    .OrderBy(
                        x =>
                            (x.Value == 0) && (GetHostType(x.Key) == HostType.Zsync)
                                ? 0
                                : 1)
                    .ToArray();
            }

            return hosts.OrderBy(x => x.Value).ToArray();
        }
    }
}