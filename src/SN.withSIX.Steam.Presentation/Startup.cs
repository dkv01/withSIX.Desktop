// <copyright company="SIX Networks GmbH" file="Startup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Owin;
using Owin;
using SN.withSIX.Core.Applications;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Infra.Api;
using SN.withSIX.Mini.Plugin.Arma.Models;
using withSIX.Api.Models.Extensions;
using withSIX.Steam.Plugin.Arma;
using Unit = System.Reactive.Unit;

namespace SN.withSIX.Steam.Presentation
{
    class Startup
    {
        public void Configuration(IAppBuilder app) {
            app.UseCors(new MyCorsOptions());
            app.Map("/api", api => {
                api.AddPath<GetServerInfo, ServerInfo>("/get-server-info");
                api.AddPath<GetEvents, EventsModel>("/get-events");
            });
        }
    }

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

    public class GetServerInfo : IAsyncQuery<ServerInfo>
    {
        public Guid GameId { get; set; }
        public List<IPEndPoint> Addresses { get; set; }
        public bool IncludeRules { get; set; }
        public bool IncludeDetails { get; set; }
    }

    public static class Raiser
    {
        public static IEventStorage Raiserr { get; set; } = new EventStorage();
        public static Task Raise(this IEvent evt) => Raiserr.AddEvent(evt);
    }

    public class GetServerInfoHandler : IAsyncRequestHandler<GetServerInfo, ServerInfo>
    {
        private readonly ISteamApi _steamApi;

        public GetServerInfoHandler(ISteamApi steamApi) {
            _steamApi = steamApi;
        }

        public async Task<ServerInfo> Handle(GetServerInfo message) {
            using (var sb = await SteamActions.CreateServerBrowser(_steamApi).ConfigureAwait(false)) {
                using (var cts = new CancellationTokenSource()) {
                    var builder = ServerFilterBuilder.Build();
                    if ((message.Addresses != null) && message.Addresses.Any())
                        builder.FilterByAddresses(message.Addresses);
                    var filter = builder.Value;
                    var obs = await (message.IncludeDetails
                        ? sb.GetServersInclDetails(cts.Token, filter, message.IncludeRules)
                        : sb.GetServers(cts.Token, filter));
                    var r =
                        await
                            obs
                                .SelectMany(async x => {
                                    await new ReceivedServerEvent(x).Raise().ConfigureAwait(false);
                                    return x;
                                })
                                .Take(10) // todo config limit
                                .ToList();
                    cts.Cancel();
                    return new ServerInfo {Servers = r};
                }
            }
        }
    }

    public class RemoteEvent<T> : RemoteEventData where T : IEvent
    {
        public RemoteEvent(T evt) : base(evt.GetType().AssemblyQualifiedName) {
            Data = evt.ToJson();
        }
    }

    public class ServerInfo
    {
        public ICollection<ArmaServerInfo> Servers { get; set; }
    }

    internal static class BuilderExtensions
    {
        internal static readonly Excecutor Executor = new Excecutor();

        public static IAppBuilder AddPath<T>(this IAppBuilder content, string path)
            where T : IAsyncRequest<Unit>
        => content.AddPath<T, Unit>(path);

        public static IAppBuilder AddPath<T, TResponse>(this IAppBuilder content, string path)
            where T : IAsyncRequest<TResponse>
        => content.Map(path, builder => builder.Run(ExecuteRequest<T, TResponse>));

        static Task ExcecuteVoidCommand<T>(IOwinContext context) where T : IAsyncRequest<Unit>
        => ExecuteRequest<T, Unit>(context);

        static Task ExecuteRequest<T, TOut>(IOwinContext context) where T : IAsyncRequest<TOut>
        =>
            context.ProcessRequest<T, TOut>(
                request => Executor.ApiAction(() => Executor.SendAsync(request), request,
                    CreateException));

        private static Exception CreateException(string s, Exception exception)
            => new UnhandledUserException(s, exception);

        internal static Task ProcessRequest<T>(this IOwinContext context, Func<T, Task> handler)
            => context.ProcessRequest<T, string>(async d => {
                await handler(d).ConfigureAwait(false);
                return "";
            });

        internal static async Task ProcessRequest<T, TOut>(this IOwinContext context, Func<T, Task<TOut>> handler) {
            using (var memoryStream = new MemoryStream()) {
                T request;
                if (context.Request.Method.ToLower() == "get") {
                    var str = context.Request.Query.ToDictionary(x => x.Key, x => x.Value).ToJson();
                    request = str.FromJson<T>();
                } else {
                    await context.Request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);
                    var str = Encoding.UTF8.GetString(memoryStream.ToArray());
                    request = str.FromJson<T>();
                }
                var returnValue = await handler(request).ConfigureAwait(false);
                await context.RespondJson(returnValue).ConfigureAwait(false);
            }
        }

        internal static async Task RespondJson(this IOwinContext context, object returnValue) {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(returnValue.ToJson()).ConfigureAwait(false);
        }
    }
}