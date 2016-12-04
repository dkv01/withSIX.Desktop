// <copyright company="SIX Networks GmbH" file="UpdateHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Core.Applications.Services;
using withSIX.Core.Presentation;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Presentation.Wpf.Services;

namespace withSIX.Mini.Presentation.Wpf
{
    public class UpdateHandler : IUpdateHandler, IPresentationService
    {
        private readonly IRestarter _restarter;
        private readonly ISquirrelUpdater _squirrel;

        public UpdateHandler(ISquirrelUpdater squirrel, IRestarter restarter) {
            _squirrel = squirrel;
            _restarter = restarter;
        }

        public async Task SelfUpdate() {
            var updateApp = await _squirrel.UpdateApp(p => { }).ConfigureAwait(false);
            _restarter.RestartInclEnvironmentCommandLine();
        }
    }
}