// <copyright company="SIX Networks GmbH" file="Startup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Cors;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Owin;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Infra.Api.Hubs;

namespace SN.withSIX.Mini.Infra.Api
{
    public class Startup : IUsecaseExecutor
    {
        static Startup() {
            var serializer = CreateJsonSerializer();
            GlobalHost.DependencyResolver.Register(typeof (JsonSerializer), () => serializer);
            var resolver = new Resolver(serializer);
            GlobalHost.DependencyResolver.Register(typeof (IParameterResolver), () => resolver);
            GlobalHost.HubPipeline.AddModule(new HubErrorLoggingPipelineModule());
        }

        public static IDisposable Start(IPEndPoint http, IPEndPoint https) {
            var startOptions = new StartOptions();
            if (http == null && https == null)
                throw new CannotOpenApiPortException("No HTTP or HTTPS ports available");
            if (http != null)
                startOptions.Urls.Add(http.ToHttp());
            if (https != null)
                startOptions.Urls.Add(https.ToHttps());
            return WebApp.Start<Startup>(startOptions);
        }

        private static JsonSerializer CreateJsonSerializer()
            => JsonSerializer.Create(new JsonSerializerSettings().SetDefaultSettings());

        public void Configuration(IAppBuilder app) {
            app.UseCors(new MyCorsOptions());
            app.Map("/api", api => {
                api.Map("/get-upload-folders",
                    builder =>
                        builder.Run(
                            context =>
                                Load<List<string>, List<FolderInfo>>(context,
                                    folders => this.RequestAsync(new GetFolders(folders)))));
                api.Map("/whitelist-upload-folders",
                    builder => builder.Run(context => Load<List<string>, object>(context,
                        async folders => {
                            await this.RequestAsync(new WhiteListFolders(folders)).ConfigureAwait(false);
                            return "";
                        })));
            });
            app.Map("/signalr", map => {
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
            });
            app.Map("", builder => builder.Run(async ctx => ctx.Response.Redirect("https://withsix.com")));
        }

        static async Task Load<T, TOut>(IOwinContext context, Func<T, Task<TOut>> handler) {
            context.Response.ContentType = "application/json";
            using (var memoryStream = new MemoryStream()) {
                await context.Request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);
                var folders = Tools.Serialization.Json.LoadJson<T>(Encoding.UTF8.GetString(memoryStream.ToArray()));
                var returnValue = await handler(folders).ConfigureAwait(false);
                await
                    context.Response.WriteAsync(JsonConvert.SerializeObject(returnValue,
                        SerializationExtension.DefaultSettings)).ConfigureAwait(false);
            }
        }

        class Resolver : DefaultParameterResolver
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

                var json = (string) _valueField.GetValue(value);
                using (var reader = new StringReader(json))
                    return _serializer.Deserialize(reader, descriptor.ParameterType);
            }
        }
    }

    public class MyCorsPolicyProvider : CorsPolicyProvider
    {
        public MyCorsPolicyProvider() {
            PolicyResolver = context => {
                var policy = new CorsPolicy();
                ConfigurePolicy(policy);
                return Task.FromResult(policy);
            };
        }

        static void ConfigurePolicy(CorsPolicy policy) {
            foreach (var host in Environments.Origins)
                policy.Origins.Add(host);

            policy.AllowAnyMethod = true;
            policy.AllowAnyHeader = true;
            policy.SupportsCredentials = true;
        }
    }

    public class MyCorsOptions : CorsOptions
    {
        public MyCorsOptions() {
            PolicyProvider = new MyCorsPolicyProvider();
        }
    }
}