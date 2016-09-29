// <copyright company="SIX Networks GmbH" file="MediatorLoggingDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Logging;

namespace withSIX.Core.Presentation.Decorators
{
    // We don't log the notification object because notifications can contain complex objects and huge hierarchies..
    public class MediatorLoggingDecorator : MediatorDecoratorBase, IMediator
    {
        protected static readonly JsonSerializerSettings JsonSerializerSettings = CreateJsonSerializerSettings();

        public MediatorLoggingDecorator(IMediator decorated) : base(decorated) {}

        public override TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            using (
                Decorated.Bench(
                    startMessage:
                    "Writes: " + (request is IWrite) + ", Data: " +
                    JsonConvert.SerializeObject(request, JsonSerializerSettings),
                    caller: "Request" + ": " + request.GetType()))
                return base.Send(request);
        }

        public override async Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            using (Decorated.Bench(
                startMessage:
                "Writes: " + (request is IWrite) + ", Data: " +
                JsonConvert.SerializeObject(request, JsonSerializerSettings),
                caller: "RequestAsync" + ": " + request.GetType()))
                return await base.SendAsync(request).ConfigureAwait(false);
        }

        public override async Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) {
            using (Decorated.Bench(
                startMessage:
                "Writes: " + (request is IWrite) + ", Data: " +
                JsonConvert.SerializeObject(request, JsonSerializerSettings),
                caller: "RequestAsync" + ": " + request.GetType()))
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        static JsonSerializerSettings CreateJsonSerializerSettings() {
            var settings = new JsonSerializerSettings().SetDefaultSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            return settings;
        }
    }
}