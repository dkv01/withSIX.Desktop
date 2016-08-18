// <copyright company="SIX Networks GmbH" file="InstallCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using SN.withSIX.Steam.Api;
using SN.withSIX.Steam.Api.Services;

namespace SN.withSIX.Steam.Presentation.Commands
{
    public class InstallCommand : BaseSteamCommand
    {
        private readonly ISteamApi _steamApi;
        private readonly ISteamDownloader _steamDownloader;

        public InstallCommand(ISteamSessionFactory factory, ISteamDownloader steamDownloader, ISteamApi steamApi)
            : base(factory) {
            _steamDownloader = steamDownloader;
            _steamApi = steamApi;

            IsCommand("install", "Install desired Steam PublishedFile(s) for the specified appid");
            HasFlag("f|force", "Force resubscription", f => Force = f);
            AllowsAnyAdditionalArguments("<publishedfileid> [<publishedfileid2> ...]");
        }

        public bool Force { get; private set; }

        protected override async Task<int> RunAsync(string[] pIds) {
            if (!pIds.Any()) {
                Console.WriteLine("Please specify at least 1 publishedfileid");
                return 11;
            }
            using (await StartSession().ConfigureAwait(false)) {
                foreach (var nfo in pIds)
                    await ProcessContent(nfo).ConfigureAwait(false);
            }
            return 0;
        }

        private async Task ProcessContent(string nfo) {
            ulong p;
            var force = Force;
            if (nfo.StartsWith("!")) {
                force = true;
                p = Convert.ToUInt64(nfo.Substring(1));
            } else
                p = Convert.ToUInt64(nfo);

            Console.WriteLine($"Starting {p}");
            var pf = new PublishedFile(p, AppId);
            await
                pf.Download(_steamDownloader, _steamApi, (l, d) => Console.WriteLine($"{l}/s {d}%"), force: force)
                    .ConfigureAwait(false);
            Console.WriteLine($"Finished {p}");
        }
    }
}