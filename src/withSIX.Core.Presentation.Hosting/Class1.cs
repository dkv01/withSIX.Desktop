// <copyright company="SIX Networks GmbH" file="Class1.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;

namespace withSIX.Core.Presentation.Hosting
{
    public static class MimeTypes
    {
        public const string JsonContentType = "application/json";
        public const string GzipContentType = "application/x-gzip";
    }

    public static class RequestHandling
    {
        private static readonly string[] methods = {"GET", "HEAD", "DELETE", "TRACE"};

        public static async Task<T> GetRequestData<T>(this HttpContext context) {
            T requestData;
            var contextRequest = context.Request;
            if (contextRequest.ShouldIncludeBody() && HasBody(contextRequest))
                using (var s = new StreamReader(contextRequest.Body)) {
                    var input = await s.ReadToEndAsync().ConfigureAwait(false);
                    requestData = input.FromJson<T>();
                    if (requestData == null)
                        throw new Exception("The request body object was somehow null!");
                }
            else
                requestData = /* context.Request.Query.MapTo<T>(); */ Activator.CreateInstance<T>();

            // TODO: Query string (hard with nested objects and arrays etc)
            var routeData = context.GetRouteData();
            if (routeData?.Values != null) {
                var js = routeData.Values.ToJson();
                JsonConvert.PopulateObject(js, requestData, JsonSupport.DefaultSettings);
            }
            return requestData;
        }

        private static bool HasBody(HttpRequest contextRequest)
            => contextRequest.Body != Stream.Null && contextRequest.ContentLength != 0;

        public static bool ShouldIncludeBody(this HttpRequest contextRequest)
            => !methods.ContainsIgnoreCase(contextRequest.Method);

        public static Task RespondJson(this HttpContext context, object returnValue) {
            context.Response.ContentType = MimeTypes.JsonContentType;
            return context.Response.WriteAsync(returnValue.ToJson());
        }
    }
}