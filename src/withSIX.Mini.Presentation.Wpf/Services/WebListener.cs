using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using withSIX.Core.Extensions;
using withSIX.Core.Presentation;
using withSIX.Mini.Presentation.Core;
using withSIX.Mini.Presentation.Core.Services;
using withSIX.Mini.Presentation.Owin.Core;

namespace withSIX.Mini.Presentation.Wpf.Services
{
    public class WebListener : WebServerStartup, IPresentationService
    {
        public WebListener(IEnumerable<OwinModule> modules) : base(modules) {}

        protected override void ConfigureBuilder(IWebHostBuilder builder) => builder.UseKestrel(kestrel => {
            kestrel.ThreadCount = 20; // Due to tabs etc..
            using (var s = WindowsApiPortHandlerBase.GetApiStream("server.pfx"))
                kestrel.UseHttps(new X509Certificate2(s.ToBytes(), "localhost"));
        });

        public override void Run(IPEndPoint http, IPEndPoint https, CancellationToken ct) {
            try {
                base.Run(http, https, ct);
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
}