﻿// <copyright company="SIX Networks GmbH" file="WebListener.cs">
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using withSIX.Core.Extensions;
using withSIX.Core.Presentation;
using withSIX.Mini.Infra.Api;
using withSIX.Mini.Presentation.Core;
using withSIX.Mini.Presentation.Core.Services;
using withSIX.Mini.Presentation.Owin.Core;

namespace withSIX.Mini.Presentation.Wpf.Services
{
    public class WebListener : WebServerStartup, IPresentationService
    {
        protected override void ConfigureBuilder(IWebHostBuilder builder) {
            builder.UseKestrel(kestrel => {
                kestrel.ThreadCount = 20; // Due to tabs etc..
                using (var s = WindowsApiPortHandlerBase.GetApiStream("server.pfx"))
                    kestrel.UseHttps(new X509Certificate2(s.ToBytes(), "localhost"));
            });
            builder.UseStartup<Startup>();
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

    public class Startup : AspStartupBase
    {
        //public Startup(IHostingEnvironment env) : base(env) {}

        public override void ConfigureServices(IServiceCollection services) {
            services.AddCors();
            services.ConfigureSignalrServices();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IMyHubHost hubHost) { // , ILoggerFactory loggerFactory,
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();

            //app.UseApplicationInsightsRequestTelemetry();

            //app.UseApplicationInsightsExceptionTelemetry();

            //app.UseMvc();
            app.ConfigureCors();
            app.ConfigureApi();
            app.ConfigureSignalr(hubHost);
            app.ConfigureCatchAll();
        }
    }
}