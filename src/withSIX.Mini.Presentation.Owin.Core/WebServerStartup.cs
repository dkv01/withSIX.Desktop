using System.Collections.Generic;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using withSIX.Core;
using withSIX.Core.Applications.Extensions;
using withSIX.Mini.Presentation.Core;

namespace withSIX.Mini.Presentation.Owin.Core
{
    public abstract class WebServerStartup : IWebServerStartup
    {
        private readonly IEnumerable<OwinModule> _modules;

        protected WebServerStartup(IEnumerable<OwinModule> modules) {
            _modules = modules;
        }
        protected virtual void Configure(IApplicationBuilder app) {
            // , IHostingEnvironment env, ILoggerFactory loggerFactory
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

            foreach (var m in _modules)
                m.Configure(app);
        }

        public virtual void Run(IPEndPoint http, IPEndPoint https, CancellationToken cancelToken) {
            var config = new ConfigurationBuilder()
                //.AddCommandLine(args)
                .Build();

            var builder = new WebHostBuilder()
                //        .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(config);
            builder.ConfigureServices(ConfigureServices);
            ConfigureBuilder(builder);

            var urls = new List<string>();
            if ((http == null) && (https == null))
                throw new CannotOpenApiPortException("No HTTP or HTTPS ports available");
            if (http != null)
                urls.Add(http.ToHttp());
            if (https != null)
                urls.Add(https.ToHttps());

            builder.Configure(Configure);
            builder.UseUrls(urls.ToArray());
            //builder.UseStartup<Startup>();

            builder.Build().Run(cancelToken);
        }

        protected abstract void ConfigureBuilder(IWebHostBuilder builder);

        protected virtual void ConfigureServices(IServiceCollection obj) {
            obj.AddCors();
            foreach (var m in _modules)
                m.ConfigureServices(obj);
        }
    }

    public class Startup
    {
        public Startup(IHostingEnvironment env) {
            var builder = new ConfigurationBuilder();
                //.SetBasePath(env.ContentRootPath)
                //.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsEnvironment("Development")) {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                //builder.AddApplicationInsightsSettings(developerMode: true);
            }

            builder.AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services) {
            // Add framework services.
            //services.AddApplicationInsightsTelemetry(Configuration);

            //services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        /*
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
    
            //app.UseApplicationInsightsRequestTelemetry();
    
            //app.UseApplicationInsightsExceptionTelemetry();
    
            //app.UseMvc();
        }
        */
    }
}