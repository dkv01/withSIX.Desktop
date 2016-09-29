// <copyright company="SIX Networks GmbH" file="EventStorage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using withSIX.Core.Applications;
using withSIX.Core.Helpers;

namespace withSIX.Steam.Presentation
{
    public interface IEventStorage
    {
        Task AddEvent<T>(T evt) where T : IEvent;
        Task<List<IEvent>> DrainEvents();
    }

    public class EventStorage : IEventStorage
    {
        private readonly AsyncLock _l = new AsyncLock();
        List<IEvent> Events { get; } = new List<IEvent>();

        public async Task AddEvent<T>(T evt) where T : IEvent {
            using (await _l.LockAsync().ConfigureAwait(false))
                Events.Add(evt);
        }

        public async Task<List<IEvent>> DrainEvents() {
            using (await _l.LockAsync().ConfigureAwait(false)) {
                var evt = Events.ToList();
                Events.Clear();
                return evt;
            }
        }
    }

    public static class EventStorageExtensions
    {
        public static async Task<List<IEvent>> DrainUntilHasEvents(this IEventStorage storage) {
            while (true) {
                var evt = await storage.DrainEvents().ConfigureAwait(false);
                if (evt.Any())
                    return evt;
                await Task.Delay(100).ConfigureAwait(false);
            }
        }
    }
}