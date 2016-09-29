using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using withSIX.Core.Extensions;
using withSIX.Core.Presentation;
using withSIX.Mini.Presentation.Core;
using withSIX.Mini.Presentation.Core.Services;
using withSIX.Mini.Presentation.Owin.Core;

namespace withSIX.Mini.Presentation.Electron
{
    public class WebListener : WebServerStartup, IPresentationService
    {
        public WebListener(IEnumerable<OwinModule> modules) : base(modules) {}

        protected override void ConfigureBuilder(IWebHostBuilder builder) => builder.UseKestrel(kestrel => {
            kestrel.ThreadCount = 20; // Due to tabs etc..
            using (var s = WindowsApiPortHandlerBase.GetApiStream("server.pfx"))
                kestrel.UseHttps(new X509Certificate2(s.ToBytes(), "localhost"));
        });

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
}