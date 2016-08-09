// <copyright company="SIX Networks GmbH" file="DefaultDomainEventHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Applications.Extensions;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    public class DefaultDomainEventHandler : IDomainEventHandler
    {
        readonly ConcurrentDictionary<object, List<ISyncDomainEvent>> _handlers =
            new ConcurrentDictionary<object, List<ISyncDomainEvent>>();

        public void PrepareEvent(object obj, ISyncDomainEvent evt) {
            var list = GetList(obj);
            list.Add(evt);
        }

        public async Task RaiseEvents() {
            foreach (var obj in _handlers.Select(x => x.Key))
                await RaiseEvents(obj).ConfigureAwait(false);
        }

        async Task RaiseEvents(object obj) {
            List<ISyncDomainEvent> list;
            if (_handlers.TryRemove(obj, out list))
                await list.RaiseEvents().ConfigureAwait(false);
        }

        List<ISyncDomainEvent> GetList(object obj) => _handlers.GetOrAdd(obj, reference => new List<ISyncDomainEvent>());
    }

    internal static class EventExtensions
    {
        public static async Task RaiseEvents(this IEnumerable<ISyncDomainEvent> events) {
            foreach (var evt in events)
                await evt.RaiseEvent().ConfigureAwait(false);
        }

        private static Task RaiseEvent(this ISyncDomainEvent evt) {
            // Dynamic to fix the generic type use in the mediator...
            dynamic notification = evt;
            return MediatorExtensions.Raise(notification);
        }
    }
}