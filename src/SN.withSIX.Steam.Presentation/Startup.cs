// <copyright company="SIX Networks GmbH" file="Startup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Owin;
using Owin;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Infra.Api;
using withSIX.Steam.Plugin.Arma;
using withSIX.Api.Models.Extensions;
using Unit = System.Reactive.Unit;

namespace SN.withSIX.Steam.Presentation
{
    class Startup
    {
        public void Configuration(IAppBuilder app) {
            app.UseCors(new MyCorsOptions());
            app.Map("/api", api => { api.AddPath<GetServerInfo, ServerInfo>("/get-server-info"); });
        }
    }

    public class GetServerInfo : IAsyncQuery<ServerInfo>
    {
        public Guid GameId { get; set; }
        public List<IPEndPoint> Addresses { get; set; }
        public bool IncludeRules { get; set; }
        public bool IncludeDetails { get; set; }
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
                    if (message.Addresses != null && message.Addresses.Any())
                        builder.FilterByAddresses(message.Addresses);
                    var filter = builder.Value;
                    var obs = await (message.IncludeDetails
                        ? sb.GetServersInclDetails(cts.Token, filter, message.IncludeRules)
                        : sb.GetServers(cts.Token, filter));
                    var r =
                        await
                            obs
                                .Take(10) // todo config limit
                                .ToList();
                    cts.Cancel();
                    return new ServerInfo {Servers = r};
                }
            }
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
                await context.Request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);
                var requestData = Encoding.UTF8.GetString(memoryStream.ToArray()).FromJson<T>();
                var returnValue = await handler(requestData).ConfigureAwait(false);
                await context.RespondJson(returnValue).ConfigureAwait(false);
            }
        }

        internal static async Task RespondJson(this IOwinContext context, object returnValue) {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(returnValue.ToJson()).ConfigureAwait(false);
        }
    }
}