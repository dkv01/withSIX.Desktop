using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Presentation.Owin.Core
{
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

        internal static async Task ProcessCancellableRequest<T, TOut>(this HttpContext context, Func<T, CancellationToken, Task<TOut>> handler) {
            using (var memoryStream = new MemoryStream()) {
                await context.Request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);
                var requestData = Encoding.UTF8.GetString(memoryStream.ToArray()).FromJson<T>();
                var g = Guid.NewGuid(); // TODO: Get from request!
                var ct = Mapping.AddToken(g);
                try {
                    var returnValue = await handler(requestData, ct).ConfigureAwait(false);
                    await context.RespondJson(returnValue).ConfigureAwait(false);
                } finally {
                    Mapping.Remove(g);
                }
            }
        }

        internal static Task ProcessRequest<T>(this HttpContext context, Func<T, Task> handler)
            => context.ProcessRequest<T, string>(async d => {
                await handler(d).ConfigureAwait(false);
                return "";
            });

        internal static async Task ProcessRequest<T, TOut>(this HttpContext context, Func<T, Task<TOut>> handler) {
            using (var memoryStream = new MemoryStream()) {
                await context.Request.Body.CopyToAsync(memoryStream).ConfigureAwait(false);
                var requestData = Encoding.UTF8.GetString(memoryStream.ToArray()).FromJson<T>();
                var returnValue = await handler(requestData).ConfigureAwait(false);
                await context.RespondJson(returnValue).ConfigureAwait(false);
            }
        }

        internal static async Task RespondJson(this HttpContext context, object returnValue) {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(returnValue.ToJson()).ConfigureAwait(false);
        }
    }
}