using System;

namespace withSIX.Core.Applications.Services
{
    // TODO: We could actually make this info available in CallContext,
    // and pull it out when we need it again :S
    public interface IRequireConnectionId
    {
        string ConnectionId { get; set; }
    }

    public interface IRequireRequestId
    {
        Guid RequestId { get; set; }
    }

    public class ClientEvent<T>
    {
        public ClientEvent(T evt, string connectionId, Guid requestId) {
            Evt = evt;
            ConnectionId = connectionId;
            RequestId = requestId;
        }
        public string ConnectionId { get; }
        public Guid RequestId { get; }
        public T Evt { get; }
    }

    public static class Exts
    {
        public static ClientEvent<T> ToClientEvent<T>(this T evt, string connectionId, Guid requestId)
            => new ClientEvent<T>(evt, connectionId, requestId);

        public static ClientEvent<T> ToClientEvent<T, T2>(this T evt, T2 message)
            where T2 : IRequireConnectionId, IRequireRequestId
        => evt.ToClientEvent(message.ConnectionId, message.RequestId);
    }
}