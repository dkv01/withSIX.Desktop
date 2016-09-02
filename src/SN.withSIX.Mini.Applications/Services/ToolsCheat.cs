// <copyright company="SIX Networks GmbH" file="ToolsCheat.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Sync.Core.Legacy.Status;
using SN.withSIX.Sync.Core.Transfer;

namespace SN.withSIX.Mini.Applications.Services
{
    public interface IToolsCheat
    {
        Task SingleToolsInstallTask(CancellationToken token = default(CancellationToken));
    }

    public class ToolsCheat : IToolsCheat, IApplicationService
    {
        private readonly AsyncLock _lock = new AsyncLock();
        private readonly IToolsInstaller _toolsInstaller;
        private Task _lazy;

        public ToolsCheat(IToolsInstaller toolsInstaller) {
            _toolsInstaller = toolsInstaller;
        }

        public async Task SingleToolsInstallTask(CancellationToken token = default(CancellationToken)) {
            Task lazy;
            using (await _lock.LockAsync(token).ConfigureAwait(false)) {
                if (_lazy == null || _lazy.IsFaulted)
                    _lazy = InstallToolsIfNeeded(token);
                lazy = _lazy;
            }
            await lazy;
        }

        async Task InstallToolsIfNeeded(CancellationToken token) {
            if (await _toolsInstaller.ConfirmToolsInstalled(true).ConfigureAwait(false))
                return;
            var repo = new StatusRepo(token) {Action = RepoStatus.Downloading};
                //using (new RepoWatcher(repo))
                //using (new StatusRepoMonitor(repo, (Func<double, double, Task>)StatusChange))
            await _toolsInstaller.DownloadAndInstallTools(repo).ConfigureAwait(false);
        }
    }
}