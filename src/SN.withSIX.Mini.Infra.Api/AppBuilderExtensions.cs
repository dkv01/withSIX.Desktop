using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Owin.Builder;
using Owin;

namespace SN.withSIX.Mini.Infra.Api
{
    public static class AppBuilderExtensions
    {
        public static IApplicationBuilder UseAppBuilder(this IApplicationBuilder app, Action<IAppBuilder> configure) {
            app.UseOwin(addToPipeline => {
                addToPipeline(next => {
                    var appBuilder = new AppBuilder();
                    appBuilder.Properties["builder.DefaultApp"] = next;

                    configure(appBuilder);

                    return appBuilder.Build<Func<IDictionary<string, object>, Task>>();
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
}