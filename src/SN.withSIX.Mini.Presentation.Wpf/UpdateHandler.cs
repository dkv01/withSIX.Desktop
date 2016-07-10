// <copyright company="SIX Networks GmbH" file="UpdateHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Presentation.Wpf.Services;

namespace SN.withSIX.Mini.Presentation.Wpf
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