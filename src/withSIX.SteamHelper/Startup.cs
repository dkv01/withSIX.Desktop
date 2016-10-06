// <copyright company="SIX Networks GmbH" file="Startup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Applications;
using withSIX.Core.Extensions;
using withSIX.Core.Infra.Services;
using withSIX.Core.Presentation;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Usecases;
using withSIX.Mini.Plugin.Arma.Models;
using withSIX.Mini.Presentation.Core;
using withSIX.Mini.Presentation.Core.Services;
using withSIX.Steam.Plugin.Arma;
using Unit = System.Reactive.Unit;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Logging;
using withSIX.Steam.Presentation.Usecases;

namespace withSIX.Steam.Presentation
{
    public abstract class WebServerStartup : IWebServerStartup
    {
        public virtual Task Run(IPEndPoint http, IPEndPoint https, CancellationToken cancelToken) {
            var hostBuilder = new WebHostBuilder();
            //        .UseContentRoot(Directory.GetCurrentDirectory())
            ConfigureBuilder(hostBuilder);

            var urls = new List<string>();
            if ((http == null) && (https == null))
                throw new CannotOpenApiPortException("No HTTP or HTTPS ports available");
            if (http != null)
                urls.Add(http.ToHttp());
            if (https != null)
                urls.Add(https.ToHttps());
            hostBuilder.UseUrls(urls.ToArray());
            var webHost = hostBuilder.Build();
            return TaskExt.StartLongRunningTask(() => webHost.Run(cancelToken), cancelToken);
        }

        protected abstract void ConfigureBuilder(IWebHostBuilder builder);
    }

    public static class AspExtensions
    {
        public static void ConfigureApi(this IApplicationBuilder app) {
            app.Map("/api", api => {
                api.AddCancellablePath<GetServerInfo, ServerInfo>("/get-server-info");
                api.AddPath<GetEvents, EventsModel>("/get-events");
            });
        }

        public static void ConfigureCors(this IApplicationBuilder app) {
            app.UseCors(builder => {
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowCredentials();
                builder.WithOrigins(Environments.Origins);
            });
        }
    }
    public class WebListener : WebServerStartup, IPresentationService
    {
        protected override void ConfigureBuilder(IWebHostBuilder builder) {
            builder.UseKestrel(kestrel => {
                kestrel.ThreadCount = 20; // Due to tabs etc..
                using (var s = WindowsApiPortHandlerBase.GetApiStream("server.pfx"))
                    kestrel.UseHttps(new X509Certificate2(s.ToBytes(), "localhost"));
            });
            builder.UseStartup<AspStartup>();
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

    public abstract class AspStartupBase
    {
        protected AspStartupBase(IHostingEnvironment env) {
            var builder = new ConfigurationBuilder();
            //.SetBasePath(env.ContentRootPath)
            //.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
            /*
        if (env.IsEnvironment("Development")) {
            // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
            //builder.AddApplicationInsightsSettings(developerMode: true);
        }*/

            //builder.AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public virtual void ConfigureServices(IServiceCollection services) {
            services.AddCors();
        }

        protected static void ConfigureApp(IApplicationBuilder app, Action act) {
            // , ILoggerFactory loggerFactory,
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();

            //app.UseApplicationInsightsRequestTelemetry();

            //app.UseApplicationInsightsExceptionTelemetry();

            //app.UseMvc();
            app.Use(async (ctx, next) => {
                try {
                    await next.Invoke().ConfigureAwait(false);
                } catch (Exception ex) {
                    MainLog.Logger.FormattedErrorException(ex, "Error in Owin pipeline!");
                    throw;
                }
            });
            app.ConfigureCors();
            app.ConfigureApi();
            act();
            //app.ConfigureCatchAll();
        }
    }

    public class AspStartup : AspStartupBase
    {
        public AspStartup(IHostingEnvironment env) : base(env) { }

        public override void ConfigureServices(IServiceCollection services) {
            base.ConfigureServices(services);
            services.ConfigureSignalrServices();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IMyHubHost hubHost) {
            ConfigureApp(app, () => app.ConfigureSignalr(hubHost));
        }
    }

    public interface IMyHubHost
    {
    }

    public class MyHubHost : IMyHubHost
    {
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
            services.AddSingleton<IAssemblyLocator, HubCouldNotBeResolvedWorkaround>();
            var serializer = CreateJsonSerializer();
            services.AddSingleton(serializer);
            services.AddSingleton<IMyHubHost, MyHubHost>();
            var resolver = new Resolver(serializer);
            services.AddSingleton<IParameterResolver>(resolver);
            //var sp = services.BuildServiceProvider();
            //ConnectionManager = new HCGetter(sp);
            //HubPipeline.AddModule(new HubErrorLoggingPipelineModule());
        }

        private static JsonSerializer CreateJsonSerializer()
            => JsonSerializer.Create(new JsonSerializerSettings().SetDefaultSettings());

        internal class HubCouldNotBeResolvedWorkaround : IAssemblyLocator
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
                    return new[] { _entryAssembly }; // , typeof(HubCouldNotBeResolvedWorkaround).GetTypeInfo().Assembly
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

    internal static class BuilderExtensions
    {
        internal static readonly Excecutor Executor = new Excecutor();

        public static IApplicationBuilder AddPath<T>(this IApplicationBuilder content, string path)
            where T : IAsyncRequest<Unit>
        => content.AddPath<T, Unit>(path);

        public static IApplicationBuilder AddPath<T, TResponse>(this IApplicationBuilder content, string path)
            where T : IAsyncRequest<TResponse>
        => content.Map(path, builder => builder.Run(ExecuteRequest<T, TResponse>));

        static Task ExcecuteVoidCommand<T>(HttpContext context) where T : IAsyncRequest<Unit>
        => ExecuteRequest<T, Unit>(context);

        static Task ExecuteRequest<T, TOut>(HttpContext context) where T : IAsyncRequest<TOut>
        =>
            context.ProcessRequest<T, TOut>(
                request => Executor.ApiAction(() => Executor.SendAsync(request), request,
                    CreateException));

        public static IApplicationBuilder AddCancellablePath<T>(this IApplicationBuilder content, string path)
            where T : ICancellableAsyncRequest<Unit>
        => content.AddCancellablePath<T, Unit>(path);

        public static IApplicationBuilder AddCancellablePath<T, TResponse>(this IApplicationBuilder content, string path)
            where T : ICancellableAsyncRequest<TResponse>
        => content.Map(path, builder => builder.Run(ExecuteCancellableRequest<T, TResponse>));

        static Task ExcecuteCancellableVoidCommand<T>(HttpContext context) where T : ICancellableAsyncRequest<Unit>
        => ExecuteCancellableRequest<T, Unit>(context);

        static Task ExecuteCancellableRequest<T, TOut>(HttpContext context) where T : ICancellableAsyncRequest<TOut>
        => context.ProcessCancellableRequest<T, TOut>(
                (request, cancelToken) => Executor.ApiAction(() => Executor.SendAsync(request, cancelToken), request,
                    CreateException));

        private static Exception CreateException(string s, Exception exception)
            => new UnhandledUserException(s, exception);

        internal static Task ProcessCancellableRequest<T>(this HttpContext context,
                Func<T, CancellationToken, Task> handler)
            => context.ProcessCancellableRequest<T, string>(async (d, c) => {
                await handler(d, c).ConfigureAwait(false);
                return "";
            });

        private static readonly CancellationTokenMapping Mapping = new CancellationTokenMapping();

        internal static async Task ProcessCancellableRequest<T, TOut>(this HttpContext context,
            Func<T, CancellationToken, Task<TOut>> handler) {
            var requestData = await GetRequestData<T>(context).ConfigureAwait(false);

            var g = context.Request.Query.ContainsKey("requestId")
                ? Guid.Parse(context.Request.Query["requestId"])
                : Guid.NewGuid();
            var ct = Mapping.AddToken(g);
            try {
                var returnValue = await handler(requestData, ct).ConfigureAwait(false);
                await context.RespondJson(returnValue).ConfigureAwait(false);
            } finally {
                Mapping.Remove(g);
            }
        }

        internal static Task ProcessRequest<T>(this HttpContext context, Func<T, Task> handler)
            => context.ProcessRequest<T, string>(async d => {
                await handler(d).ConfigureAwait(false);
                return "";
            });

        internal static async Task ProcessRequest<T, TOut>(this HttpContext context, Func<T, Task<TOut>> handler) {
            var requestData = await GetRequestData<T>(context).ConfigureAwait(false);
            var returnValue = await handler(requestData).ConfigureAwait(false);
            await context.RespondJson(returnValue).ConfigureAwait(false);
        }

        private static async Task<T> GetRequestData<T>(HttpContext context) {
            T requestData;
            if (context.Request.Method.ToLower() == "get") {
                requestData = Activator.CreateInstance<T>(); // TODO: Create with Get variables
            } else {
                using (var memoryStream = new MemoryStream()) {
                    await context.Request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);
                    var body = Encoding.UTF8.GetString(memoryStream.ToArray());
                    MainLog.Logger.Debug($"Received request body: {body}\nQS: {context.Request.QueryString}");
                    requestData = body.FromJson<T>();
                    if (requestData == null)
                        throw new Exception("The request body object was somehow null!");
                }
            }
            return requestData;
        }


        internal static async Task RespondJson(this HttpContext context, object returnValue) {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(returnValue.ToJson()).ConfigureAwait(false);
        }
    }
}