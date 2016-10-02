// <copyright company="SIX Networks GmbH" file="Compression.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace withSIX.Mini.Presentation.Owin.Core
{
    namespace Middlewares
    {
        /// <summary>
        ///     Experimental compression middleware for self-hosted web app
        /// </summary>
        public class CompressionMiddleware
        {
            private const long MinimumLength = 2700;
            private readonly RequestDelegate _next;

            public CompressionMiddleware(RequestDelegate next) {
                _next = next;
            }

            public async Task Invoke(HttpContext context) {
                var acceptEncoding = context.Request.Headers["Accept-Encoding"];
                if (acceptEncoding.ToString().IndexOf("gzip", StringComparison.CurrentCultureIgnoreCase) < 0) {
                    await _next(context).ConfigureAwait(false);
                    return;
                }

                using (var buffer = new MemoryStream()) {
                    var body = context.Response.Body;
                    context.Response.Body = buffer;
                    try {
                        await _next(context).ConfigureAwait(false);

                        if (buffer.Length >= MinimumLength) {
                            using (var compressed = new MemoryStream()) {
                                using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true)) {
                                    buffer.Seek(0, SeekOrigin.Begin);
                                    await buffer.CopyToAsync(gzip).ConfigureAwait(false);
                                }

                                if (compressed.Length < buffer.Length) {
                                    // write compressed data to response
                                    context.Response.Headers.Add("Content-Encoding", new[] {"gzip"});
                                    if (context.Response.Headers["Content-Length"].Count > 0) {
                                        context.Response.Headers["Content-Length"] = compressed.Length.ToString();
                                    }

                                    compressed.Seek(0, SeekOrigin.Begin);
                                    await compressed.CopyToAsync(body).ConfigureAwait(false);
                                    return;
                                }
                            }
                        }

                        // write uncompressed data to response
                        buffer.Seek(0, SeekOrigin.Begin);
                        await buffer.CopyToAsync(body).ConfigureAwait(false);
                    } finally {
                        context.Response.Body = body;
                    }
                }
            }
        }
    }
}