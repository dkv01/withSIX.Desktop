// <copyright company="SIX Networks GmbH" file="PerformUpdate.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public enum UpdateState
    {
        UpdateDownloading,
        UpdateDownloaded
    }

    public class UpdateAvailable : IAsyncVoidCommand
    {
        public UpdateAvailable(UpdateState state, string version) {
            State = state;
            Version = version;
        }

        public UpdateState State { get; }
        public string Version { get; }
    }

    public class PerformUpdate : IAsyncVoidCommand {}

    public class PerformUpdateHandler : IAsyncVoidCommandHandler<PerformUpdate>,
        IAsyncVoidCommandHandler<UpdateAvailable>
    {
        private readonly INodeApi _nodeApi;
        readonly IRestarter _restarter;
        readonly ISquirrelUpdater _squirrel;
        private readonly IStateHandler _stateHandler;
        private readonly IContentInstallationService _contentInstallation;

        public PerformUpdateHandler(ISquirrelUpdater squirrel, IRestarter restarter, INodeApi nodeApi,
            IStateHandler stateHandler, IContentInstallationService contentInstallation) {
            _squirrel = squirrel;
            _restarter = restarter;
            _nodeApi = nodeApi;
            _stateHandler = stateHandler;
            _contentInstallation = contentInstallation;
        }

        public async Task<UnitType> HandleAsync(PerformUpdate request) {
            await _contentInstallation.Abort().ConfigureAwait(false);

            // TODO: Progress reporting etc
            await _stateHandler.StartUpdating().ConfigureAwait(false);

            if (Cheat.IsNode)
                await NodeSelfUpdate().ConfigureAwait(false);
            else
                await LegacySelfUpdate().ConfigureAwait(false);
            return UnitType.Default;
        }

        public async Task<UnitType> HandleAsync(UpdateAvailable request) {
            await
                _stateHandler.UpdateAvailable(request.Version ?? "6.6.6",
                    request.State == UpdateState.UpdateDownloading
                        ? AppUpdateState.UpdateDownloading
                        : AppUpdateState.UpdateAvailable).ConfigureAwait(false);
            return UnitType.Default;
        }

        private Task NodeSelfUpdate() => _nodeApi.InstallSelfUpdate();

        private async Task LegacySelfUpdate() {
            var updateApp = await _squirrel.UpdateApp(p => { }).ConfigureAwait(false);
            _restarter.RestartInclEnvironmentCommandLine();
        }
    }
}