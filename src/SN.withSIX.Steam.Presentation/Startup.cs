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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Plugin.Arma.Models;
using withSIX.Api.Models.Extensions;
using withSIX.Steam.Plugin.Arma;
using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;
using Unit = System.Reactive.Unit;
using SN.withSIX.Core.Infra.Services;

namespace SN.withSIX.Steam.Presentation
{
    class Startup
    {
        public static void Configure(IApplicationBuilder app) {
            app.UseCors(builder => {
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowCredentials();
                builder.WithOrigins(Environments.Origins);
            });
            app.Map("/api", api => {
                api.AddCancellablePath<GetServerInfo, ServerInfo>("/get-server-info");
                api.AddPath<GetEvents, EventsModel>("/get-events");
            });
        }

        public static IWebHost Start(string http) {
            var config = new ConfigurationBuilder()
                //.AddCommandLine(args)
                .Build();

            var builder = new WebHostBuilder()
                //        .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(config);

            builder.UseWebListener(options => { });
            builder.ConfigureServices(ConfigureServices);

            var urls = new[] {http};
            builder.Configure(Configure);
            return builder.Start(urls);
        }

        private static void ConfigureServices(IServiceCollection obj) {
            obj.AddCors();
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

    public class GetServerInfo : ICancellableQuery<ServerInfo>
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

    public class GetServerInfoHandler : ICancellableAsyncRequestHandler<GetServerInfo, ServerInfo>
    {
        private readonly ISteamApi _steamApi;

        public GetServerInfoHandler(ISteamApi steamApi) {
            _steamApi = steamApi;
        }

        public async Task<ServerInfo> Handle(GetServerInfo message, CancellationToken ct) {
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

        public static IApplicationBuilder AddPath<T>(this IApplicationBuilder content, string path)
            where T : IAsyncRequest<Unit>
        => content.AddPath<T, Unit>(path);

        public static IApplicationBuilder AddPath<T, TResponse>(this IApplicationBuilder content, string path)
            where T : IAsyncRequest<TResponse>
        => content.Map(path, builder => builder.Run(ExecuteRequest<T, TResponse>));

        static Task ExcecuteVoidCommand<T>(HttpContext context) where T : IAsyncRequest<Unit>
        => ExecuteRequest<T, Unit>(context);

        static Task ExecuteRequest<T, TOut>(HttpContext context) where T : IAsyncRequest<TOut>
        =>
            context.ProcessRequest<T, TOut>(
                request => Executor.ApiAction(() => Executor.SendAsync(request), request,
                    CreateException));

        public static IApplicationBuilder AddCancellablePath<T>(this IApplicationBuilder content, string path)
            where T : ICancellableAsyncRequest<Unit>
        => content.AddCancellablePath<T, Unit>(path);

        public static IApplicationBuilder AddCancellablePath<T, TResponse>(this IApplicationBuilder content, string path)
            where T : ICancellableAsyncRequest<TResponse>
        => content.Map(path, builder => builder.Run(ExecuteCancellableRequest<T, TResponse>));

        static Task ExcecuteCancellableVoidCommand<T>(HttpContext context) where T : ICancellableAsyncRequest<Unit>
        => ExecuteCancellableRequest<T, Unit>(context);

        static Task ExecuteCancellableRequest<T, TOut>(HttpContext context) where T : ICancellableAsyncRequest<TOut>
        => context.ProcessCancellableRequest<T, TOut>(
                (request, cancelToken) => Executor.ApiAction(() => Executor.SendAsync(request, cancelToken), request,
                    CreateException));

        private static Exception CreateException(string s, Exception exception)
            => new UnhandledUserException(s, exception);

        internal static Task ProcessCancellableRequest<T>(this HttpContext context,
                Func<T, CancellationToken, Task> handler)
            => context.ProcessCancellableRequest<T, string>(async (d, c) => {
                await handler(d, c).ConfigureAwait(false);
                return "";
            });

        private static readonly CancellationTokenMapping Mapping = new CancellationTokenMapping();

        internal static async Task ProcessCancellableRequest<T, TOut>(this HttpContext context, Func<T, CancellationToken, Task<TOut>> handler) {
            using (var memoryStream = new MemoryStream()) {
                await context.Request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);
                var requestData = Encoding.UTF8.GetString(memoryStream.ToArray()).FromJson<T>();
                var g = Guid.NewGuid(); // TODO: Get from request!
                var ct = Mapping.AddToken(g);
                try {
                    var returnValue = await handler(requestData, ct).ConfigureAwait(false);
                    await context.RespondJson(returnValue).ConfigureAwait(false);
                } finally {
                    Mapping.Remove(g);
                }
            }
        }

        internal static Task ProcessRequest<T>(this HttpContext context, Func<T, Task> handler)
            => context.ProcessRequest<T, string>(async d => {
                await handler(d).ConfigureAwait(false);
                return "";
            });

        internal static async Task ProcessRequest<T, TOut>(this HttpContext context, Func<T, Task<TOut>> handler) {
            using (var memoryStream = new MemoryStream()) {
                await context.Request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);
                var requestData = Encoding.UTF8.GetString(memoryStream.ToArray()).FromJson<T>();
                var returnValue = await handler(requestData).ConfigureAwait(false);
                await context.RespondJson(returnValue).ConfigureAwait(false);
            }
        }


        internal static async Task RespondJson(this HttpContext context, object returnValue) {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(returnValue.ToJson()).ConfigureAwait(false);
        }
    }
}