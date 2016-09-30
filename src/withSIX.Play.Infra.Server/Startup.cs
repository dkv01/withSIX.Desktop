// <copyright company="SIX Networks GmbH" file="Startup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using System.Web.Cors;
using MediatR;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Owin;
using withSIX.Api.Models.Extensions;
using withSIX.Play.Applications;
using withSIX.Play.Core.Connect;
using withSIX.Play.Infra.Server.Hubs;
using withSIX.Play.Infra.Server.UseCases;

namespace withSIX.Play.Infra.Server
{
    public class StartInternalSignalRServer : IRequest<IDisposable>
    {
        public StartInternalSignalRServer(int port = 56666) {
            Port = port;
        }

        public int Port { get; }
    }

    public class StopInternalSignalRServer : IRequest<Unit> {}

    public class StartThisServer
    {
        public IDisposable Start(IMediator mediator, IDepResolver depResolver, int port = 56666) {
            Startup.HubActivator = new SIHubActivator(depResolver);
            Startup.Mediator = mediator;
            // TODO: Allow anywhere
            return Common.Flags.Public
                ? WebApp.Start<Startup>("http://*:" + port)
                : WebApp.Start<Startup>("http://localhost:" + port);
        }
    }

    public class SIHubActivator : IHubActivator
    {
        readonly IDepResolver _container;

        public SIHubActivator(IDepResolver container) {
            _container = container;
        }

        public IHub Create(HubDescriptor descriptor) => typeof (BaseHub).IsAssignableFrom(descriptor.HubType)
            ? (IHub) _container.GetInstance(descriptor.HubType)
            : (IHub) Activator.CreateInstance(descriptor.HubType);
    }

    public class Startup
    {
        public static IHubActivator HubActivator { get; set; }
        public static IMediator Mediator { get; set; }

        static void ConfigurePolicy(CorsPolicy policy) {
            foreach (var host in Environments.Origins)
                policy.Origins.Add(host);

            policy.AllowAnyMethod = true;
            policy.AllowAnyHeader = true;
            policy.SupportsCredentials = true;
        }

        static JsonSerializer CreateJsonSerializer() => JsonSerializer.Create(new JsonSerializerSettings().SetDefaultSettings());

        
        public void Configuration(IAppBuilder app) {
            GlobalHost.DependencyResolver.Register(typeof (IHubActivator), () => HubActivator);
            GlobalHost.DependencyResolver.Register(typeof (JsonSerializer), CreateJsonSerializer);
            app.Map("/api/command", builder => builder.Run(InvokeCommand));
            app.Map("/api/games", builder => builder.Run(InvokeGames));

            // TODO: Is just a workaround for SA issue..
            app.Map("/signalr", map => map.UseCors(new MyCorsOptions()).RunSignalR(new HubConfiguration()));
        }

        Task InvokeCommand(IOwinContext context) {
            context.Response.ContentType = "text/plain";
            Cheat.PublishEvent(new ProcessAppEvent("pws://" + context.Request.QueryString));
            return context.Response.WriteAsync("Command executed");
        }

        Task InvokeGames(IOwinContext context) {
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonConvert.SerializeObject(Mediator.Send(new GetGameInfoQuery()), JsonSupport.DefaultSettings));
        }

        public class MyCorsOptions : CorsOptions
        {
            public MyCorsOptions() {
                PolicyProvider = new MyCorsPolicyProvider();
            }
        }

        public class MyCorsPolicyProvider : CorsPolicyProvider
        {
            public MyCorsPolicyProvider() {
                PolicyResolver = context => {
                    var policy = new CorsPolicy();
                    try {
                        ConfigurePolicy(policy);
                    } catch (Exception ex) {
                        MainLog.Logger.FormattedErrorException(ex, "Origin error");
                        throw;
                    }
                    return Task.FromResult(policy);
                };
            }
        }
    }
}