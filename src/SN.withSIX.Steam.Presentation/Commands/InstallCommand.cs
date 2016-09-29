// <copyright company="SIX Networks GmbH" file="InstallCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using withSIX.Steam.Api;
using withSIX.Steam.Api.Services;

namespace withSIX.Steam.Presentation.Commands
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
                Error("Please specify at least 1 publishedfileid");
                return 11;
            }
            await DoWithSteamSession(() => PerformActions(pIds)).ConfigureAwait(false);
            return 0;
        }

        private async Task PerformActions(IEnumerable<string> pIds) {
            foreach (var act in pIds.Select(ParsePid))
                await ProcessContent(act).ConfigureAwait(false);
        }

        private async Task ProcessContent(Tuple<PublishedFile, bool> act) {
            Info($"Starting {act.Item1.Pid}");
            await App.Download(_steamDownloader, _steamApi, act.Item1, (l, d) => Progress($"{l}/s {d}%"),
                    force: act.Item2 || Force)
                .ConfigureAwait(false);
            Info($"Finished {act.Item1.Pid}");
        }
    }
}