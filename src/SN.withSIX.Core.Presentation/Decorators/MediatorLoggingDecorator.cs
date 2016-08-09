// <copyright company="SIX Networks GmbH" file="MediatorLoggingDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.Core.Presentation.Decorators
{
    public abstract class LoggingDecorator
    {
        protected static readonly JsonSerializerSettings JsonSerializerSettings = CreateJsonSerializerSettings();

        static JsonSerializerSettings CreateJsonSerializerSettings() {
            var settings = new JsonSerializerSettings().SetDefaultSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            return settings;
        }
    }

    public class MediatorLoggingDecorator : LoggingDecorator, IMediator
    {
        readonly IMediator _mediator;

        public MediatorLoggingDecorator(IMediator mediator) {
            Contract.Requires<ArgumentNullException>(mediator != null);
            _mediator = mediator;
        }

        public TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            using (
                _mediator.Bench(
                    startMessage:
                        "Writes: " + (request is IWrite) + ", Data: " +
                        JsonConvert.SerializeObject(request, JsonSerializerSettings),
                    caller: "Request" + ": " + request.GetType()))
                return _mediator.Send(request);
        }

        public async Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            using (_mediator.Bench(
                startMessage:
                    "Writes: " + (request is IWrite) + ", Data: " +
                    JsonConvert.SerializeObject(request, JsonSerializerSettings),
                caller: "RequestAsync" + ": " + request.GetType()))
                return await _mediator.SendAsync(request).ConfigureAwait(false);
        }

        public void Publish(INotification notification) => _mediator.Publish(notification);

        public Task PublishAsync(IAsyncNotification notification) => _mediator.PublishAsync(notification);

        public Task PublishAsync(ICancellableAsyncNotification notification, CancellationToken cancellationToken)
            => _mediator.PublishAsync(notification, cancellationToken);

        public async Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) {
            using (_mediator.Bench(
                startMessage:
                    "Writes: " + (request is IWrite) + ", Data: " +
                    JsonConvert.SerializeObject(request, JsonSerializerSettings),
                caller: "RequestAsync" + ": " + request.GetType()))
                return await _mediator.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // We don't log the notification object because notifications can contain complex objects and huge hierarchies..
    }
}