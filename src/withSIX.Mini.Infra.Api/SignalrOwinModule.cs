using System.IO;
using System.Reflection;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNetCore.Builder;
using Newtonsoft.Json;
using Owin;
using withSIX.Api.Models.Extensions;
using withSIX.Mini.Infra.Api.Hubs;
using withSIX.Mini.Presentation.Owin.Core;

namespace withSIX.Mini.Infra.Api
{
    public class SignalrOwinModule : OwinModule
    {
        static SignalrOwinModule() {
            var serializer = CreateJsonSerializer();
            GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), () => serializer);
            var resolver = new SignalrBuilderExtensions.Resolver(serializer);
            GlobalHost.DependencyResolver.Register(typeof(IParameterResolver), () => resolver);
            GlobalHost.HubPipeline.AddModule(new HubErrorLoggingPipelineModule());

        }
        public override void Configure(IApplicationBuilder builder) => builder.UseSignalR2();

        private static JsonSerializer CreateJsonSerializer()
            => JsonSerializer.Create(new JsonSerializerSettings().SetDefaultSettings());
    }


    public static class SignalrBuilderExtensions
    {
        public static void UseSignalR2(this IApplicationBuilder app) {
            app.UseWebSockets();
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

        internal class Resolver : DefaultParameterResolver
        {
            private readonly JsonSerializer _serializer;

            private FieldInfo _valueField;

            public Resolver(JsonSerializer serializer) {
                _serializer = serializer;
            }

            public override object ResolveParameter(ParameterDescriptor descriptor, IJsonValue value) {
                if (value.GetType() == descriptor.ParameterType)
                    return value;

                if (_valueField == null)
                    _valueField = value.GetType().GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);

                var json = (string)_valueField.GetValue(value);
                using (var reader = new StringReader(json))
                    return _serializer.Deserialize(reader, descriptor.ParameterType);
            }
        }
    }
}