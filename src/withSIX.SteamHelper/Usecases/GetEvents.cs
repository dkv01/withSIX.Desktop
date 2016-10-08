using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications;
using withSIX.Core.Applications.Services;
using System.Linq;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Helpers;

namespace withSIX.Steam.Presentation.Usecases
{
    [Obsolete("Replace with S-IR/WebSockets")]
    public class GetEvents : IAsyncQuery<EventsModel> {}

    public class GetEventsHandler : IAsyncRequestHandler<GetEvents, EventsModel>
    {
        public async Task<EventsModel> Handle(GetEvents message) {
            var events = await Raiser.Raiserr.DrainUntilHasEvents().ConfigureAwait(false);
            return new EventsModel {
                Events = events.Select(x => new RemoteEvent<IEvent>(x)).ToList<RemoteEventData>()
            };
        }
    }

    public static class Raiser
    {
        public static IEventStorage Raiserr { get; set; } = new EventStorage();
        public static Task Raise(this IEvent evt) => Raiserr.AddEvent(evt);
    }

    public class RemoteEvent<T> : RemoteEventData where T : IEvent
    {
        public RemoteEvent(T evt) : base(evt.GetType().AssemblyQualifiedName) {
            Data = evt.ToJson();
        }
    }
}