// <copyright company="SIX Networks GmbH" file="CacheManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Akavache;
using withSIX.Core.Infra.Services;

namespace withSIX.Core.Infra.Cache
{
    public class CacheManager : ICacheManager, IInfrastructureService
    {
        private static readonly string vacuumKey = "____last_vacuum";
        readonly List<IBlobCache> _caches = new List<IBlobCache>();

        public Task Vacuum() {
            IBlobCache[] caches;
            lock (_caches)
                caches = _caches.ToArray();

            return caches
                .Select(x => Observable.FromAsync(() => Vacuum(x)))
                .Concat()
                .Select(_ => Unit.Default)
                .ToTask();
        }

        public Task VacuumIfNeeded(TimeSpan timeAgo) {
            IBlobCache[] caches;
            lock (_caches)
                caches = _caches.ToArray();

            return
                caches
                    .Select(x => Observable.FromAsync(() => VacuumIfNeeded(x, timeAgo)))
                    .Concat()
                    .Select(_ => Unit.Default)
                    .ToTask();
        }

        public Task Shutdown() {
            IBlobCache[] caches;
            lock (_caches)
                caches = _caches.ToArray();

            return caches.Select(x => {
                x.Dispose();
                return x.Shutdown;
            }).Merge().ToList().Select(_ => Unit.Default).ToTask();
        }

        public void RegisterCache(IBlobCache cache) {
            lock (_caches)
                _caches.Add(cache);
        }

        private static async Task Vacuum(IBlobCache x) {
            await x.Vacuum();
            await x.InsertObject(vacuumKey, DateTime.UtcNow);
        }

        private async Task VacuumIfNeeded(IBlobCache x, TimeSpan timeAgo) {
            var lastVacuum = await x.GetOrCreateObject(vacuumKey, () => new DateTime());
            if (lastVacuum > DateTime.UtcNow.Subtract(timeAgo))
                return;
            await Vacuum(x);
        }
    }
}