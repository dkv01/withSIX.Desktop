// <copyright company="SIX Networks GmbH" file="UninstallCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
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
                foreach (var nfo in pIds.Select(x => new PublishedFile(Convert.ToUInt64(x), AppId)))
                    await ProcessContent(nfo).ConfigureAwait(false);
            }
            return 0;
        }

        private async Task ProcessContent(PublishedFile pf) {
            Console.WriteLine($"Starting {pf.Pid}");
            await pf.Uninstall(_steamApi).ConfigureAwait(false);
            Console.WriteLine($"Finished {pf.Pid}");
        }
    }
}