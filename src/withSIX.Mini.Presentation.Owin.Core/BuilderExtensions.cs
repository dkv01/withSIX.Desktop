﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Infra.Services;
using withSIX.Core.Logging;
using withSIX.Core.Presentation;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Features;
using withSIX.Mini.Applications.Services;

namespace withSIX.Mini.Presentation.Owin.Core
{
    public static class BuilderExtensions
    {
        public static IApplicationBuilder AddPath<T>(this IApplicationBuilder content, string path)
            where T : IRequest
            => content.Map(path, builder => builder.Run(ExecuteRequest<T>));

        public static IApplicationBuilder AddPath<T, TResponse>(this IApplicationBuilder content, string path)
            where T : IRequest<TResponse>
        => content.Map(path, builder => builder.Run(ExecuteRequest<T, TResponse>));

        public static IApplicationBuilder AddCancellablePath<T>(this IApplicationBuilder content, string path)
            where T : IRequest<Unit>
        => content.AddCancellablePath<T, Unit>(path);

        public static IApplicationBuilder AddCancellablePath<T, TResponse>(this IApplicationBuilder content, string path)
            where T : IRequest<TResponse>
        => content.Map(path, builder => builder.Run(ExecuteCancellableRequest<T, TResponse>));

        static Task ExcecuteVoidCommand<T>(HttpContext context) where T : IRequest<Unit>
        => ExecuteRequest<T, Unit>(context);

        static Task ExecuteRequest<T, TOut>(HttpContext context) where T : IRequest<TOut>
        =>
            context.ProcessRequest<T, TOut>(
                request => A.ApiAction(ct => A.Excecutor.Send(request, cancelToken: ct), request,
                    CreateException, GetRequestId(context), null, context.User, RequestScopeService.Instance));

        static Task ExecuteRequest<T>(HttpContext context) where T : IRequest
            =>
                context.ProcessRequest<T>(
                    request => A.ApiAction(ct => A.Excecutor.Send(request, cancelToken: ct), request,
                        CreateException, GetRequestId(context), null, context.User, RequestScopeService.Instance));


        static Task ExecuteCancellableRequest<T, TOut>(HttpContext context) where T : IRequest<TOut>
        => context.ProcessRequest<T, TOut>(
            request => A.ApiAction(ct => A.Excecutor.Send(request, ct), request,
                CreateException, GetRequestId(context), null, context.User, RequestScopeService.Instance));

        private static Guid GetRequestId(HttpContext context) => context.Request.Query.ContainsKey("requestId")
            ? Guid.Parse(context.Request.Query["requestId"])
            : Guid.NewGuid();

        private static Exception CreateException(string s, Exception exception)
            => new UnhandledUserException(s, exception);

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