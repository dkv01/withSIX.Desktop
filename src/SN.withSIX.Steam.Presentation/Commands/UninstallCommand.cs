// <copyright company="SIX Networks GmbH" file="UninstallCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Steam.Api;
using SN.withSIX.Steam.Api.Services;

namespace SN.withSIX.Steam.Presentation.Commands
{
    public class UninstallCommand : BaseSteamCommand
    {
        private readonly ISteamApi _steamApi;

        public UninstallCommand(ISteamSessionFactory factory, ISteamApi steamApi) : base(factory) {
            _steamApi = steamApi;
            IsCommand("uninstall", "Uninstall desired Steam PublishedFile(s) for the specified appid");
        }

        protected override async Task<int> RunAsync(string[] pIds) {
            if (!pIds.Any()) {
                Console.WriteLine("Please specify at least 1 publishedfileid");
                return 11;
            }
            using (await StartSession().ConfigureAwait(false)) {
                var app = new App(AppId);
                foreach (var pid in pIds.Select(x => app.GetPf(Convert.ToUInt64(x))))
                    await ProcessContent(app, pid).ConfigureAwait(false);
            }
            return 0;
        }

        private async Task ProcessContent(App app, PublishedFile pf) {
            Console.WriteLine($"Starting {pf.Pid}");
            await app.Uninstall(pf, _steamApi).ConfigureAwait(false);
            Console.WriteLine($"Finished {pf.Pid}");
        }
    }
}