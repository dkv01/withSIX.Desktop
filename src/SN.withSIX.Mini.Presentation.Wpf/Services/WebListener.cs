// <copyright company="SIX Networks GmbH" file="WebListener.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Core.Presentation.Services;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Presentation.Core;
using SN.withSIX.Mini.Presentation.Owin.Core;

namespace SN.withSIX.Mini.Presentation.Wpf.Services
{
    public class WebStart : WindowsWebStartBase, IInitializer
    {
        public WebStart(IProcessManager pm, IDialogManager dm, IExitHandler exitHandler) : base(pm, dm, exitHandler) { }
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