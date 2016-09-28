// <copyright company="SIX Networks GmbH" file="Startup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Builder;
using Newtonsoft.Json;
using Owin;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Applications.Usecases.Main.Games;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Infra.Api.Hubs;
using withSIX.Api.Models.Extensions;

using Microsoft.Owin.Builder;
using Microsoft.Owin.Logging;
using SN.withSIX.Core;
using SN.withSIX.Core.Infra.Services;
using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;


namespace SN.withSIX.Mini.Infra.Api
{
    public class Startup
    {
        static Startup() {
            var serializer = CreateJsonSerializer();
            GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), () => serializer);
            var resolver = new BuilderExtensions.Resolver(serializer);
            GlobalHost.DependencyResolver.Register(typeof(IParameterResolver), () => resolver);
            GlobalHost.HubPipeline.AddModule(new HubErrorLoggingPipelineModule());
        }

        public static void Configure(IApplicationBuilder app) { // , IHostingEnvironment env, ILoggerFactory loggerFactory
                                                         //loggerFactory.AddConsole();

            //if (env.IsDevelopment()) {
            //  app.UseDeveloperExceptionPage();
            //}

            app.UseCors(builder => {
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowCredentials();
                builder.WithOrigins(Environments.Origins);
            });

            app.UseSignalR2();
            Configuration(app);
            //app.Run(async (context) => {
              //  await context.Response.WriteAsync("Hello World!");
            //});
        }

        public static IDisposable Start(IPEndPoint http, IPEndPoint https) {

            var config = new ConfigurationBuilder()
                //.AddCommandLine(args)
                .Build();

            var builder = new WebHostBuilder()
                //        .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(config);

            builder.UseWebListener(options => { });
            builder.ConfigureServices(ConfigureServices);

            var urls = new List<string>();
            if ((http == null) && (https == null))
                throw new CannotOpenApiPortException("No HTTP or HTTPS ports available");
            if (http != null)
                urls.Add(http.ToHttp());
            if (https != null)
                urls.Add(https.ToHttps());

            builder.Configure(Configure);
            return builder.Start(urls.ToArray());
        }

        private static void ConfigureServices(IServiceCollection obj) {
            obj.AddCors();
        }

        private static JsonSerializer CreateJsonSerializer()
            => JsonSerializer.Create(new JsonSerializerSettings().SetDefaultSettings());

        static void Configuration(IApplicationBuilder app) {
            app.Map("/api", api => {
                api.Map("/version",
                    builder => builder.Run(context => context.RespondJson(new {Version = Consts.ApiVersion})));

                api.AddPath<PingPlugin>("/ping-plugin");

                api.Map("/external-downloads", content => {
                    content.AddPath<ExternalDownloadStarted, Guid>("/started");
                    content.AddPath<ExternalDownloadProgressing>("/progressing");
                    content.AddPath<AddExternalModRead>("/completed");
                    content.AddPath<StartDownloadSession>("/start-session");
                });

                api.Map("/content", content => {
                    content.AddPath<InstallContent>("/install-content");
                    content.AddPath<InstallContents>("/install-contents");
                    content.AddPath<InstallSteamContents>("/install-steam-contents");
                    content.AddPath<UninstallContent>("/uninstall-content");
                    content.AddPath<UninstallContents>("/uninstall-contents");
                    content.AddPath<LaunchContent>("/launch-content");
                    content.AddPath<LaunchContents>("/launch-contents");
                    content.AddPath<CloseGame>("/close-game");

                    // Deprecate
                    content.AddPath<AddExternalModRead>("/add-external-mod");
                });

                api.Map("/get-upload-folders",
                    builder =>
                        builder.Run(
                            context =>
                                context.ProcessRequest<List<string>, List<FolderInfo>>(
                                    folders => BuilderExtensions.Executor.SendAsync(new GetFolders(folders)))));

                api.Map("/whitelist-upload-folders",
                    builder =>
                        builder.Run(
                            context =>
                                context.ProcessRequest<List<string>>(
                                    folders => BuilderExtensions.Executor.SendAsync(new WhiteListFolders(folders)))));
            });

            app.Map("", builder => builder.Run(async ctx => ctx.Response.Redirect("https://withsix.com")));
        }
    }


    public static class AppBuilderExtensions
    {
        public static IApplicationBuilder UseAppBuilder(this IApplicationBuilder app, Action<IAppBuilder> configure) {
            app.UseOwin(addToPipeline => {
                addToPipeline(next => {
                    var appBuilder = new AppBuilder();
                    appBuilder.Properties["builder.DefaultApp"] = next;

                    configure(appBuilder);

                    return appBuilder.Build<AppFunc>();
                });
            });

            return app;
        }

        public static void UseSignalR2(this IApplicationBuilder app) {
            app.UseAppBuilder(appBuilder => 
            appBuilder.Map("/signalr", map => {
                var debug =
#if DEBUG
                    true;
#else
                    false;
#endif

                var hubConfiguration = new HubConfiguration {
                    EnableDetailedErrors = debug
                };

                // Run the SignalR pipeline. We're not using MapSignalR
                // since this branch is already runs under the "/signalr"
                // path.
                map.RunSignalR(hubConfiguration);
            }));
        }
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