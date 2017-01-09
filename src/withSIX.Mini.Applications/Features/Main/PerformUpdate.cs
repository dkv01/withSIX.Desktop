// <copyright company="SIX Networks GmbH" file="PerformUpdate.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace withSIX.Mini.Applications.Features.Main
{
    public enum UpdateState
    {
        UpdateDownloading,
        UpdateDownloaded
    }

    public class UpdateAvailable : ICommand
    {
        public UpdateAvailable(UpdateState state, string version) {
            State = state;
            Version = version;
        }

        public UpdateState State { get; }
        public string Version { get; }
    }

    public class PerformUpdate : ICommand {}

    public interface IUpdateHandler
    {
        Task SelfUpdate();
    }

    public class PerformUpdateHandler : IAsyncRequestHandler<PerformUpdate>,
        IAsyncRequestHandler<UpdateAvailable>
    {
        private readonly IContentInstallationService _contentInstallation;
        private readonly IStateHandler _stateHandler;
        private readonly IUpdateHandler _updateHandler;

        public PerformUpdateHandler(IStateHandler stateHandler,
            IContentInstallationService contentInstallation, IUpdateHandler updateHandler) {
            _stateHandler = stateHandler;
            _contentInstallation = contentInstallation;
            _updateHandler = updateHandler;
        }

        public async Task Handle(PerformUpdate request) {
            await _contentInstallation.Abort().ConfigureAwait(false);

            // TODO: Progress reporting etc
            await _stateHandler.StartUpdating().ConfigureAwait(false);
            await _updateHandler.SelfUpdate().ConfigureAwait(false);
        }

        public async Task Handle(UpdateAvailable request) {
            await
                _stateHandler.UpdateAvailable(request.Version ?? "6.6.6",
                    request.State == UpdateState.UpdateDownloading
                        ? AppUpdateState.UpdateDownloading
                        : AppUpdateState.UpdateAvailable).ConfigureAwait(false);
        }
    }
}