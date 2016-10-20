using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Applications.Extensions;
using withSIX.Mini.Applications;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Usecases;
using withSIX.Mini.Applications.Usecases.Main;
using withSIX.Mini.Applications.Usecases.Main.Games;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Presentation.Core;

namespace withSIX.Mini.Presentation.Owin.Core
{
    public abstract class WebServerStartup : IWebServerStartup
    {
        public virtual Task Run(IPEndPoint http, IPEndPoint https, CancellationToken cancelToken) {
            var urls = BuildUrls(http, https);

            var hostBuilder = new WebHostBuilder();
            //        .UseContentRoot(Directory.GetCurrentDirectory())
            ConfigureBuilder(hostBuilder);
            hostBuilder.UseUrls(urls.ToArray());
            var webHost = hostBuilder.Build();
            return TaskExt.StartLongRunningTask(() => webHost.Run(cancelToken), cancelToken);
        }

        private static List<string> BuildUrls(IPEndPoint http, IPEndPoint https) {
            var urls = new List<string>();
            if ((http == null) && (https == null))
                throw new CannotOpenApiPortException("No HTTP or HTTPS ports available");
            if (http != null)
                urls.Add(http.ToHttp());
            if (https != null)
                urls.Add(https.ToHttps());
            return urls;
        }

        protected abstract void ConfigureBuilder(IWebHostBuilder builder);
    }

    public static class AspExtensions
    {
        public static void ConfigureCors(this IApplicationBuilder app) {
            app.UseCors(builder => {
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowCredentials();
                builder.WithOrigins(Environments.Origins);
            });
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
            app.ConfigureCors();
            app.ConfigureApi();
            act();
            app.ConfigureCatchAll();
        }
    }

    public static class Extensions
    {
        public static void ConfigureApi(this IApplicationBuilder app) {
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
                                    folders => A.Excecutor.SendAsync(new GetFolders(folders)))));

                api.Map("/whitelist-upload-folders",
                    builder =>
                        builder.Run(
                            context =>
                                context.ProcessRequest<List<string>>(
                                    folders => A.Excecutor.SendAsync(new WhiteListFolders(folders)))));
            });
        }

        public static void ConfigureCatchAll(this IApplicationBuilder app) {
            app.Map("", builder => builder.Run(async ctx => ctx.Response.Redirect("https://withsix.com")));
        }
    }
}