// <copyright company="SIX Networks GmbH" file="EventStorage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SN.withSIX.Core.Applications;
using SN.withSIX.Core.Helpers;

namespace SN.withSIX.Steam.Presentation
{
    public interface IEventStorage
    {
        Task AddEvent(IEvent evt);
        Task<List<IEvent>> DrainEvents();
    }

    public class EventStorage : IEventStorage
    {
        private readonly AsyncLock _l = new AsyncLock();
        List<IEvent> Events { get; } = new List<IEvent>();

        public async Task AddEvent(IEvent evt) {
            using (await _l.LockAsync().ConfigureAwait(false)) {
                Events.Add(evt);
            }
        }

        public async Task<List<IEvent>> DrainEvents() {
            using (await _l.LockAsync().ConfigureAwait(false)) {
                var evt = Events.ToList();
                Events.Clear();
                return evt;
            }
        }
    }
}