// <copyright company="SIX Networks GmbH" file="PingPlugin.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    [Flags]
    public enum Browser
    {
        None,
        Chrome = 1,
        Firefox = 2,
        Edge = 4
    }

    public class PingPlugin : IAsyncVoidCommand
    {
        public PingPlugin(Browser browser) {
            Browser = browser;
        }

        public Browser Browser { get; }
    }

    public class PingPluginHandler : DbRequestBase, IAsyncVoidCommandHandler<PingPlugin>
    {
        public PingPluginHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<Unit> Handle(PingPlugin message) {
            if (Consts.PluginBrowserFound == Browser.None)
                Consts.PluginBrowserFound = message.Browser;
            else
                Consts.PluginBrowserFound &= message.Browser;
            return Unit.Value;
        }
    }
}