// <copyright company="SIX Networks GmbH" file="SignalrOwinModule.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Presentation.Bridge;
using withSIX.Mini.Infra.Api.Hubs;

namespace withSIX.Mini.Infra.Api
{
    public interface IMyHubHost
    {
        IHubContext<StatusHub, IStatusClientHub> StatusHub { get; }
        IHubContext<ClientHub, IClientClientHub> ClientHub { get; }
        IHubContext<ContentHub, IContentClientHub> ContentHub { get; }
        IHubContext<QueueHub, IQueueClientHub> QueueHub { get; }
        IHubContext<ServerHub, IServerHubClient> ServerHub { get; }
    }

    public class MyHubHost : IMyHubHost
    {
        public MyHubHost(IHubContext<StatusHub, IStatusClientHub> statusHub,
            IHubContext<ClientHub, IClientClientHub> clientHub, IHubContext<ContentHub, IContentClientHub> contentHub,
            IHubContext<QueueHub, IQueueClientHub> queueHub, IHubContext<ServerHub, IServerHubClient> serverHub) {
            StatusHub = statusHub;
            ClientHub = clientHub;
            ContentHub = contentHub;
            QueueHub = queueHub;
            ServerHub = serverHub;
        }

        public IHubContext<StatusHub, IStatusClientHub> StatusHub { get; }

        public IHubContext<ClientHub, IClientClientHub> ClientHub { get; }
        public IHubContext<ContentHub, IContentClientHub> ContentHub { get; }
        public IHubContext<QueueHub, IQueueClientHub> QueueHub { get; }
        public IHubContext<ServerHub, IServerHubClient> ServerHub { get; }
    }

    public static class Extensions
    {
        public static IMyHubHost ConnectionManager { get; private set; }

        public static void ConfigureSignalr(this IApplicationBuilder app, IMyHubHost hubHost) {
            ConnectionManager = hubHost;
            app.UseWebSockets();
            app.Map("/signalr", map => {
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

        public static void ConfigureSignalrServices(this IServiceCollection services) {
            services.AddSignalR();
            services.AddSingleton<IAssemblyLocator, MyAssemblyLocator>();
            var serializer = CreateJsonSerializer();
            services.AddSingleton(serializer);
            services.AddSingleton<IMyHubHost, MyHubHost>();
            var resolver = new Resolver(serializer);
            services.AddSingleton<IParameterResolver>(resolver);
            //var sp = services.BuildServiceProvider();
            //ConnectionManager = new HCGetter(sp);
            //HubPipeline.AddModule(new HubErrorLoggingPipelineModule());
        }

        public static JsonSerializer CreateJsonSerializer()
            => JsonSerializer.Create(Bridge.OtherSettings());
        /*
        public class HubCouldNotBeResolvedWorkaround : IAssemblyLocator
        {
            private static readonly string AssemblyRoot = typeof(Hub).GetTypeInfo().Assembly.GetName().Name;
            private readonly DependencyContext _dependencyContext;
            private readonly Assembly _entryAssembly;

            public HubCouldNotBeResolvedWorkaround(IHostingEnvironment environment) {
                _entryAssembly = Assembly.Load(new AssemblyName(environment.ApplicationName));
                _dependencyContext = DependencyContext.Load(_entryAssembly);
            }

            public virtual IList<Assembly> GetAssemblies() {
                if (_dependencyContext == null) {
                    // Use the entry assembly as the sole candidate.
                    return new[] { _entryAssembly, typeof(HubCouldNotBeResolvedWorkaround).GetTypeInfo().Assembly }; // , typeof(HubCouldNotBeResolvedWorkaround).GetTypeInfo().Assembly
                }

                return _dependencyContext
                    .RuntimeLibraries
                    .Where(IsCandidateLibrary)
                    .SelectMany(l => l.GetDefaultAssemblyNames(_dependencyContext))
                    .Select(assembly => Assembly.Load(new AssemblyName(assembly.Name)))
                    .ToArray();
            }

            private bool IsCandidateLibrary(RuntimeLibrary library) {
                return
                    library.Dependencies.Any(
                        dependency => string.Equals(AssemblyRoot, dependency.Name, StringComparison.Ordinal));
            }
        }
        */

        public class Resolver : DefaultParameterResolver
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

    public class MyAssemblyLocator : IAssemblyLocator
    {
        public IList<Assembly> GetAssemblies() => new List<Assembly> {typeof(ClientHub).GetTypeInfo().Assembly};
    }
}