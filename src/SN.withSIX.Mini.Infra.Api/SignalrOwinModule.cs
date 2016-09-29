using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.Builder;
using Newtonsoft.Json;
using SN.withSIX.Mini.Infra.Api.Hubs;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Infra.Api
{
    public class SignalrOwinModule : OwinModule
    {
        static SignalrOwinModule() {
            var serializer = CreateJsonSerializer();
            GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), () => serializer);
            var resolver = new BuilderExtensions.Resolver(serializer);
            GlobalHost.DependencyResolver.Register(typeof(IParameterResolver), () => resolver);
            GlobalHost.HubPipeline.AddModule(new HubErrorLoggingPipelineModule());

        }
        public override void Configure(IApplicationBuilder builder) => builder.UseSignalR2();

        private static JsonSerializer CreateJsonSerializer()
            => JsonSerializer.Create(new JsonSerializerSettings().SetDefaultSettings());
    }
}