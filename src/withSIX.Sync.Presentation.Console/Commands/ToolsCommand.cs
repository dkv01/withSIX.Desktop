// <copyright company="SIX Networks GmbH" file="ToolsCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;

using withSIX.Core.Applications.Services;
using withSIX.Sync.Core.Legacy.Status;
using withSIX.Sync.Presentation.Console.Services;

namespace withSIX.Sync.Presentation.Console.Commands
{

    public class ToolsCommand : BaseCommandAsync
    {
        readonly IToolsInstaller _toolsInstaller;

        public ToolsCommand(IToolsInstaller toolsInstaller) {
            _toolsInstaller = toolsInstaller;
            IsCommand("tools", "Download Six Tools through SynQ");
        }

        protected override async Task<int> RunAsync(params string[] remainingArguments) {
            var statusRepo = new StatusRepo();
            using (new ConsoleProgress(statusRepo))
                await _toolsInstaller.DownloadAndInstallTools(statusRepo).ConfigureAwait(false);

            return 0;
        }
    }
}