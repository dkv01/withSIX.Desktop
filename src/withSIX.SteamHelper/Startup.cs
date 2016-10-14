// <copyright company="SIX Networks GmbH" file="Startup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Hubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Extensions;
using withSIX.Core.Helpers;
using withSIX.Core.Logging;
using withSIX.Core.Presentation;
using withSIX.Mini.Presentation.Core;
using withSIX.Mini.Presentation.Core.Services;
using withSIX.Mini.Presentation.Owin.Core;
using withSIX.Steam.Presentation.Hubs;
using withSIX.Steam.Presentation.Usecases;
using static withSIX.Mini.Infra.Api.Extensions;

namespace withSIX.Steam.Presentation
{
    public abstract class WebServerStartup : IWebServerStartup
    {
        public virtual Task Run(IPEndPoint http, IPEndPoint https, CancellationToken cancelToken) {
            var hostBuilder = new WebHostBuilder();
            //        .UseContentRoot(Directory.GetCurrentDirectory())
            ConfigureBuilder(hostBuilder);

            var urls = new List<string>();
            if ((http == null) && (https == null))
                throw new CannotOpenApiPortException("No HTTP or HTTPS ports available");
            if (http != null)
                urls.Add(http.ToHttp());
            if (https != null)
                urls.Add(https.ToHttps());
            hostBuilder.UseUrls(urls.ToArray());
            var webHost = hostBuilder.Build();
            return TaskExt.StartLongRunningTask(() => webHost.Run(cancelToken), cancelToken);
        }

        protected abstract void ConfigureBuilder(IWebHostBuilder builder);
    }

    public static class AspExtensions
    {
        public static void ConfigureApi(this IApplicationBuilder app) {
            app.Map("/api", api => {
                api.AddCancellablePath<GetServerAddresses, BatchResult>("/get-server-addresses");
                api.AddCancellablePath<GetServers, BatchResult>("/get-servers");
                api.AddPath<GetEvents, EventsModel>("/get-events");
            });
        }

        public static void ConfigureCors(this IApplicationBuilder app) {
            app.UseCors(builder => {
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowCredentials();
                builder.WithOrigins(Environments.Origins);
            });
        }
    }

    public class WebListener : WebServerStartup, IPresentationService
    {
        protected override void ConfigureBuilder(IWebHostBuilder builder) {
            builder.UseKestrel(kestrel => {
                kestrel.ThreadCount = 20; // Due to tabs etc..
                using (var s = WindowsApiPortHandlerBase.GetApiStream("server.pfx"))
                    kestrel.UseHttps(new X509Certificate2(s.ToBytes(), "localhost"));
            });
            builder.UseStartup<AspStartup>();
        }

        public override async Task Run(IPEndPoint http, IPEndPoint https, CancellationToken ct) {
            try {
                await base.Run(http, https, ct).ConfigureAwait(false);
            } catch (TargetInvocationException ex) {
                var unwrapped = ex.UnwrapExceptionIfNeeded();
                throw new ListenerException(ex.Message, unwrapped);
            } catch (Exception ex) {
                throw new ListenerException(ex.Message, ex);
            }
            // todo
            //throw;
            //if (!(unwrapped is HttpListenerException))
            //  throw;
            //throw new ListenerException(ex.Message, unwrapped);
            //} catch (HttpListenerException ex) {
            //throw new ListenerException(ex.Message, ex);
        }
    }

    public abstract class AspStartupBase
    {
        protected AspStartupBase(IHostingEnvironment env) {
            var builder = new ConfigurationBuilder();
            //.SetBasePath(env.ContentRootPath)
            //.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
            /*
        if (env.IsEnvironment("Development")) {
            // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
            //builder.AddApplicationInsightsSettings(developerMode: true);
        }*/

            //builder.AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public virtual void ConfigureServices(IServiceCollection services) {
            services.AddCors();
        }

        protected static void ConfigureApp(IApplicationBuilder app, Action act) {
            // , ILoggerFactory loggerFactory,
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();

            //app.UseApplicationInsightsRequestTelemetry();

            //app.UseApplicationInsightsExceptionTelemetry();

            //app.UseMvc();
            app.Use(async (ctx, next) => {
                try {
                    await next.Invoke().ConfigureAwait(false);
                } catch (Exception ex) {
                    MainLog.Logger.FormattedErrorException(ex, "Error in Owin pipeline!");
                    throw;
                }
            });
            app.ConfigureCors();
            app.ConfigureApi();
            act();
            //app.ConfigureCatchAll();
        }
    }

    public class AspStartup : AspStartupBase
    {
        public AspStartup(IHostingEnvironment env) : base(env) {}

        public override void ConfigureServices(IServiceCollection services) {
            base.ConfigureServices(services);
            services.ConfigureSignalrServices();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IMyHubHost hubHost) {
            ConfigureApp(app, () => app.ConfigureSignalr(hubHost));
        }
    }

    public interface IMyHubHost
    {
        IHubContext<ServerHub, IServerHubClient> ServerHub { get; }
    }

    public class MyHubHost : IMyHubHost
    {
        public MyHubHost(IHubContext<ServerHub, IServerHubClient> serverHub) {
            ServerHub = serverHub;
        }

        public IHubContext<ServerHub, IServerHubClient> ServerHub { get; }
    }

    public static class Extensions
    {
        public static IMyHubHost ConnectionManager { get; private set; }

        public static void ConfigureSignalr(this IApplicationBuilder app, IMyHubHost hubHost) {
            ConnectionManager = hubHost;
            app.UseWebSockets();
            app.Map("/signalr", map => {
                var debug =
#if DEBUG
                    true;
#else
                    false;
#endif

                //var hubConfiguration = new HubConfiguration {
                //EnableDetailedErrors = debug
                //};

                // Run the SignalR pipeline. We're not using MapSignalR
                // since this branch is already runs under the "/signalr"
                // path.
                map.RunSignalR();
            });
        }

        public static void ConfigureSignalrServices(this IServiceCollection services) {
            services.AddSignalR();
            services.AddSingleton<IAssemblyLocator, MyAssemblyLocator>();
            var serializer = CreateJsonSerializer();
            services.AddSingleton(serializer);
            services.AddSingleton<IMyHubHost, MyHubHost>();
            var resolver = new Resolver(serializer);
            services.AddSingleton<IParameterResolver>(resolver);
            //var sp = services.BuildServiceProvider();
            //ConnectionManager = new HCGetter(sp);
            //HubPipeline.AddModule(new HubErrorLoggingPipelineModule());
        }
    }

    public class MyAssemblyLocator : IAssemblyLocator
    {
        public IList<Assembly> GetAssemblies() => new List<Assembly> { typeof(ServerHub).GetTypeInfo().Assembly };
    }
}