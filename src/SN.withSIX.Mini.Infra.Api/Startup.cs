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
using ShortBus;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Applications.Usecases.Main.Games;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Infra.Api.Hubs;

namespace SN.withSIX.Mini.Infra.Api
{
    public class Startup : IUsecaseExecutor
    {
        private readonly Excecutor _executor = new Excecutor();
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
                api.Map("/version", builder => {
                    builder.Run(context => RespondJson(context, new { Version = Consts.ApiVersion}));
                });
                api.Map("/content", content => {
                    content.Map("/install-content", builder => builder.Run(ExcecuteVoidCommand<InstallContent>));
                    content.Map("/install-contents", builder => builder.Run(ExcecuteVoidCommand<InstallContents>));
                    content.Map("/install-steam-contents", builder => builder.Run(ExcecuteVoidCommand<InstallSteamContents>));
                    content.Map("/uninstall-content", builder => builder.Run(ExcecuteVoidCommand<UninstallContent>));
                    content.Map("/uninstall-contents", builder => builder.Run(ExcecuteVoidCommand<UninstallContents>));
                    content.Map("/launch-content", builder => builder.Run(ExcecuteVoidCommand<LaunchContent>));
                    content.Map("/launch-contents", builder => builder.Run(ExcecuteVoidCommand<LaunchContents>));
                    content.Map("/close-game", builder => builder.Run(ExcecuteVoidCommand<CloseGame>));
                });

                api.Map("/get-upload-folders",
                    builder =>
                        builder.Run(
                            context =>
                                ProcessRequest<List<string>, List<FolderInfo>>(context,
                                    folders => this.RequestAsync(new GetFolders(folders)))));

                api.Map("/whitelist-upload-folders",
                    builder =>
                        builder.Run(
                            context =>
                                ProcessRequest<List<string>>(context, folders => this.RequestAsync(new WhiteListFolders(folders)))));
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

        Task ExcecuteVoidCommand<T>(IOwinContext context) where T : IAsyncRequest<UnitType>
            => ExecuteRequest<T, UnitType>(context);

        Task ExecuteRequest<T, TOut>(IOwinContext context) where T : IAsyncRequest<TOut> where TOut : class
            => ProcessRequest<T, TOut>(context,
                request => _executor.ApiAction(() => this.RequestAsync(request), request,
                    CreateException));

        private Exception CreateException(string s, Exception exception) => new UnhandledUserException(s, exception);

        static Task ProcessRequest<T>(IOwinContext context, Func<T, Task> handler) => ProcessRequest<T, string>(context, async d => {
            await handler(d).ConfigureAwait(false);
            return "";
        });

        static async Task ProcessRequest<T, TOut>(IOwinContext context, Func<T, Task<TOut>> handler) {
            using (var memoryStream = new MemoryStream()) {
                await context.Request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);
                var requestData = Encoding.UTF8.GetString(memoryStream.ToArray()).FromJson<T>();
                var returnValue = await handler(requestData).ConfigureAwait(false);
                await RespondJson(context, returnValue).ConfigureAwait(false);
            }
        }

        private static async Task RespondJson(IOwinContext context, object returnValue) {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(returnValue.ToJson()).ConfigureAwait(false);
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