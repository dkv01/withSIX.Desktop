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
using MediatR;
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
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Infra.Api
{
    internal static class BuilderExtensions
    {
        internal static readonly Excecutor Executor = new Excecutor();

        public static IAppBuilder AddPath<T>(this IAppBuilder content, string path) where T : IAsyncRequest<Unit>
            => content.AddPath<T, Unit>(path);

        public static IAppBuilder AddPath<T, TResponse>(this IAppBuilder content, string path)
            where T : IAsyncRequest<TResponse>
            => content.Map(path, builder => builder.Run(ExecuteRequest<T, TResponse>));

        static Task ExcecuteVoidCommand<T>(IOwinContext context) where T : IAsyncRequest<Unit>
            => ExecuteRequest<T, Unit>(context);

        static Task ExecuteRequest<T, TOut>(IOwinContext context) where T : IAsyncRequest<TOut>
            =>
                context.ProcessRequest<T, TOut>(
                    request => Executor.ApiAction(() => Executor.SendAsync(request), request,
                        CreateException));

        private static Exception CreateException(string s, Exception exception) => new UnhandledUserException(s, exception);

        internal static Task ProcessRequest<T>(this IOwinContext context, Func<T, Task> handler) => context.ProcessRequest<T, string>(async d => {
            await handler(d).ConfigureAwait(false);
            return "";
        });

        internal static async Task ProcessRequest<T, TOut>(this IOwinContext context, Func<T, Task<TOut>> handler) {
            using (var memoryStream = new MemoryStream()) {
                await context.Request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);
                var requestData = Encoding.UTF8.GetString(memoryStream.ToArray()).FromJson<T>();
                var returnValue = await handler(requestData).ConfigureAwait(false);
                await context.RespondJson(returnValue).ConfigureAwait(false);
            }
        }



        internal static async Task RespondJson(this IOwinContext context, object returnValue) {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(returnValue.ToJson()).ConfigureAwait(false);
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

    public class Startup
    {
        static Startup() {
            var serializer = CreateJsonSerializer();
            GlobalHost.DependencyResolver.Register(typeof (JsonSerializer), () => serializer);
            var resolver = new BuilderExtensions.Resolver(serializer);
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
                api.Map("/version", builder => builder.Run(context => context.RespondJson(new { Version = Consts.ApiVersion})));

                api.AddPath<PingPlugin>("/ping-plugin");

                api.Map("/content", content => {
                    content.AddPath<AddExternalModRead>("/add-external-mod");
                    content.AddPath<InstallContent>("/install-content");
                    content.AddPath<InstallContents>("/install-contents");
                    content.AddPath<InstallSteamContents>("/install-steam-contents");
                    content.AddPath<UninstallContent>("/uninstall-content");
                    content.AddPath<UninstallContents>("/uninstall-contents");
                    content.AddPath<LaunchContent>("/launch-content");
                    content.AddPath<LaunchContents>("/launch-contents");
                    content.AddPath<CloseGame>("/close-game");
                });

                api.Map("/get-upload-folders",
                    builder =>
                        builder.Run(
                            context =>
                                context.ProcessRequest<List<string>, List<FolderInfo>>(folders => BuilderExtensions.Executor.SendAsync(new GetFolders(folders)))));

                api.Map("/whitelist-upload-folders",
                    builder =>
                        builder.Run(
                            context => context.ProcessRequest<List<string>>(folders => BuilderExtensions.Executor.SendAsync(new WhiteListFolders(folders)))));
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