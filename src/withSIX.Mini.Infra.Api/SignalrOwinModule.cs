using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using Microsoft.AspNetCore.SignalR.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;
using withSIX.Mini.Presentation.Owin.Core;

namespace withSIX.Mini.Infra.Api
{
    public class SignalrOwinModule : OwinModule
    {
        public override void ConfigureServices(IServiceCollection services) {
            services.AddSignalR();
            services.AddSingleton<IAssemblyLocator, HubCouldNotBeResolvedWorkaround>();
            var serializer = CreateJsonSerializer();
            services.AddSingleton<JsonSerializer>(serializer);
            var resolver = new Resolver(serializer);
            services.AddSingleton<IParameterResolver>(resolver);
            var sp = services.BuildServiceProvider();
            ConnectionManager = sp.GetService<IConnectionManager>();
            //HubPipeline.AddModule(new HubErrorLoggingPipelineModule());
        }

        public static IConnectionManager ConnectionManager { get; private set; }

        public override void Configure(IApplicationBuilder builder) {
            //builder.UseSignalR2();
            builder.UseWebSockets();
            builder.Map("/signalr", map => {
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

        private static JsonSerializer CreateJsonSerializer()
            => JsonSerializer.Create(new JsonSerializerSettings().SetDefaultSettings());
    }

    /*
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
    }
    */


    public class HubCouldNotBeResolvedWorkaround : IAssemblyLocator
    {
        private static readonly string AssemblyRoot = typeof(Hub).GetTypeInfo().Assembly.GetName().Name;
        private readonly Assembly _entryAssembly;
        private readonly DependencyContext _dependencyContext;

        public HubCouldNotBeResolvedWorkaround(IHostingEnvironment environment) {
            _entryAssembly = Assembly.Load(new AssemblyName(environment.ApplicationName));
            _dependencyContext = DependencyContext.Load(_entryAssembly);
        }

        public virtual IList<Assembly> GetAssemblies() {
            if (_dependencyContext == null) {
                // Use the entry assembly as the sole candidate.
                return new[] { _entryAssembly, typeof(HubCouldNotBeResolvedWorkaround).GetTypeInfo().Assembly };
            }

            return _dependencyContext
                .RuntimeLibraries
                .Where(IsCandidateLibrary)
                .SelectMany(l => l.GetDefaultAssemblyNames(_dependencyContext))
                .Select(assembly => Assembly.Load(new AssemblyName(assembly.Name)))
                .ToArray();
        }

        private bool IsCandidateLibrary(RuntimeLibrary library) {
            return library.Dependencies.Any(dependency => string.Equals(AssemblyRoot, dependency.Name, StringComparison.Ordinal));
        }
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