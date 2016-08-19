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
            AllowsAnyAdditionalArguments("<publishedfileid> [<publishedfileid2> ...]");
        }

        protected override async Task<int> RunAsync(string[] pIds) {
            if (!pIds.Any()) {
                Error("Please specify at least 1 publishedfileid");
                return 11;
            }
            using (await StartSession().ConfigureAwait(false)) {
                foreach (var act in pIds.Select(ParsePid))
                    await ProcessContent(act).ConfigureAwait(false);
            }
            return 0;
        }

        private async Task ProcessContent(Tuple<PublishedFile, bool> act) {
            Info($"Starting {act.Item1.Pid}");
            await App.Uninstall(act.Item1, _steamApi).ConfigureAwait(false);
            Info($"Finished {act.Item1.Pid}");
        }
    }
}