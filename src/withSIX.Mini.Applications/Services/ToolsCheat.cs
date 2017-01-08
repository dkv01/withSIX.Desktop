// <copyright company="SIX Networks GmbH" file="ToolsCheat.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Core.Applications.Services;
using withSIX.Core.Helpers;
using withSIX.Core.Logging;
using withSIX.Sync.Core.Legacy.Status;
using withSIX.Sync.Core.Transfer;

namespace withSIX.Mini.Applications.Services
{
    public interface IToolsCheat
    {
        Task SingleToolsInstallTask(CancellationToken token = default(CancellationToken));
    }

    public class ToolsCheat : IToolsCheat, IApplicationService, IDisposable
    {
        private readonly AsyncLock _lock = new AsyncLock();
        private readonly IToolsInstaller _toolsInstaller;
        private Task _lazy;

        public ToolsCheat(IToolsInstaller toolsInstaller) {
            _toolsInstaller = toolsInstaller;
        }

        public void Dispose() {
            _lock.Dispose();
        }

        public async Task SingleToolsInstallTask(CancellationToken token = default(CancellationToken)) {
            using (this.Bench("Awaiting tools install"))
                await SingleToolsInstallTaskInternal(token).ConfigureAwait(false);
        }

        private async Task SingleToolsInstallTaskInternal(CancellationToken token) {
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