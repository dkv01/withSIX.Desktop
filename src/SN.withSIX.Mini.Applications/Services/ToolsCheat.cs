// <copyright company="SIX Networks GmbH" file="ToolsCheat.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Sync.Core.Legacy.Status;
using SN.withSIX.Sync.Core.Transfer;

namespace SN.withSIX.Mini.Applications.Services
{
    public interface IToolsCheat
    {
        Task SingleToolsInstallTask();
    }

    public class ToolsCheat : IToolsCheat, IApplicationService
    {
        private readonly AsyncLock _lock = new AsyncLock();
        private readonly IToolsInstaller _toolsInstaller;
        private Task _lazy;

        public ToolsCheat(IToolsInstaller toolsInstaller) {
            _toolsInstaller = toolsInstaller;
        }

        public async Task SingleToolsInstallTask() {
            Task lazy;
            using (await _lock.LockAsync().ConfigureAwait(false)) {
                if (_lazy == null || _lazy.IsFaulted)
                    _lazy = InstallToolsIfNeeded();
                lazy = _lazy;
            }
            await lazy;
        }

        async Task InstallToolsIfNeeded() {
            if (await _toolsInstaller.ConfirmToolsInstalled(true).ConfigureAwait(false))
                return;
            using (var repo = new StatusRepo {Action = RepoStatus.Downloading})
                //using (new RepoWatcher(repo))
                //using (new StatusRepoMonitor(repo, (Func<double, double, Task>)StatusChange))
                await _toolsInstaller.DownloadAndInstallTools(repo).ConfigureAwait(false);
        }
    }
}