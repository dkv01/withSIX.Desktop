// <copyright company="SIX Networks GmbH" file="MediatorLoggingDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
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

        public override async Task<TResponseData> Send<TResponseData>(IRequest<TResponseData> request,
            CancellationToken cancelToken = default(CancellationToken)) {
            if (request == null) throw new ArgumentNullException(nameof(request));
            using (Decorated.Bench(
                startMessage:
                "Writes: " + (request is IWrite) + ", Data: " +
                JsonConvert.SerializeObject(request, JsonSerializerSettings),
                caller: "RequestAsync" + ": " + request.GetType()))
                return await base.Send(request, cancelToken).ConfigureAwait(false);
        }

        public override async Task Send(IRequest request,
            CancellationToken cancelToken = default(CancellationToken))
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            using (Decorated.Bench(
                startMessage:
                "Writes: " + (request is IWrite) + ", Data: " +
                JsonConvert.SerializeObject(request, JsonSerializerSettings),
                caller: "RequestAsync" + ": " + request.GetType()))
                await base.Send(request, cancelToken).ConfigureAwait(false);
        }

        static JsonSerializerSettings CreateJsonSerializerSettings() {
            var settings = new JsonSerializerSettings().SetDefaultSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            return settings;
        }
    }
}