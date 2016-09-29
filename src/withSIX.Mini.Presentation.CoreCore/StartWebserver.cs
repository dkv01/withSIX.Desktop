using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Presentation.Owin.Core;

namespace withSIX.Mini.Presentation.CoreCore
{
    // http://dotnetthoughts.net/how-to-setup-https-on-kestrel/
    public class WebListener : WebServerStartup, IPresentationService
    {
        public WebListener(IEnumerable<OwinModule> modules) : base(modules) { }

        protected override void ConfigureBuilder(IWebHostBuilder builder) {
            builder.UseKestrel();
        }

        protected override void Configure(IApplicationBuilder app) {
            base.Configure(app);
            //var pfxFile = Path.Combine(appEnv.ApplicationBasePath, "Sample.pfx");
            //X509Certificate2 certificate = new X509Certificate2(pfxFile, "Password");
            //app.Use(ChangeContextToHttps);
            //app.UseKestrelHttps(certificate);
        }

        public override void Run(IPEndPoint http, IPEndPoint https, CancellationToken ct) {
            try {
                base.Run(http, https, ct);
            } catch (TargetInvocationException ex) {
                var unwrapped = ex.UnwrapExceptionIfNeeded();
                throw;
                // todo
                //if (!(unwrapped is HttpListenerException))
                //  throw;
                //throw new ListenerException(ex.Message, unwrapped);
                //} catch (HttpListenerException ex) {
                //throw new ListenerException(ex.Message, ex);
            }
        }
    }
}
