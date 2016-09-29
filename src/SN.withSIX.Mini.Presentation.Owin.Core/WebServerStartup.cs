using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Mini.Presentation.Core;

namespace SN.withSIX.Mini.Presentation.Owin.Core
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
                urls.Add(https.ToHttp());

            builder.Configure(Configure);
            builder.UseUrls(urls.ToArray());

            builder.Build().Run(cancelToken);
        }

        protected abstract void ConfigureBuilder(IWebHostBuilder builder);

        protected virtual void ConfigureServices(IServiceCollection obj) => obj.AddCors();
    }
}