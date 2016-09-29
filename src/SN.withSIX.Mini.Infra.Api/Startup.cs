// <copyright company="SIX Networks GmbH" file="Startup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SN.withSIX.Core;

namespace SN.withSIX.Mini.Infra.Api
{
    public class WebServerStartup
    {
        void Configure(IApplicationBuilder app, IEnumerable<OwinModule> modules) {
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

            foreach (var m in modules)
                m.Configure(app);
        }

        public IDisposable Start(IPEndPoint http, IPEndPoint https, params OwinModule[] modules)
            => Start(http, https, (IEnumerable<OwinModule>) modules);

        public IDisposable Start(IPEndPoint http, IPEndPoint https, IEnumerable<OwinModule> modules) {
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

            builder.Configure(app => Configure(app, modules));
            return builder.Start(urls.ToArray());
        }

        private void ConfigureServices(IServiceCollection obj) => obj.AddCors();
    }
}