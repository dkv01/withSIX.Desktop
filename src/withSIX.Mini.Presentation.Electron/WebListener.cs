// <copyright company="SIX Networks GmbH" file="WebListener.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Core.Presentation;
using withSIX.Core.Presentation.Services;
using withSIX.Core.Services.Infrastructure;
using withSIX.Mini.Presentation.Core;
using withSIX.Mini.Presentation.Owin.Core;

namespace withSIX.Mini.Presentation.Electron
{
    public class WebStart : WindowsWebStartBase, IInitializer {
        public WebStart(IProcessManager pm, IDialogManager dm, IExitHandler exitHandler) : base(pm, dm, exitHandler) {}
    }

    public class WebListener : WebServerStartup, IPresentationService
    {
        public WebListener(IEnumerable<OwinModule> modules) : base(modules) {}

        protected override void ConfigureBuilder(IWebHostBuilder builder) => builder.UseWebListener(options => { });

        public override void Run(IPEndPoint http, IPEndPoint https, CancellationToken ct) {
            try {
                base.Run(http, https, ct);
            } catch (TargetInvocationException ex) {
                var unwrapped = ex.UnwrapExceptionIfNeeded();
                if (!(unwrapped is HttpListenerException))
                    throw;
                throw new ListenerException(ex.Message, unwrapped);
            } catch (HttpListenerException ex) {
                throw new ListenerException(ex.Message, ex);
            }
        }
    }
}