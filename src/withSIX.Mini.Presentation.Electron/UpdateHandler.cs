// <copyright company="SIX Networks GmbH" file="UpdateHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Core.Presentation;
using withSIX.Mini.Applications.Features.Main;

namespace withSIX.Mini.Presentation.Electron
{
    public class UpdateHandler : IUpdateHandler, IPresentationService
    {
        private readonly INodeApi _api;

        public UpdateHandler(INodeApi api) {
            _api = api;
        }

        public Task SelfUpdate() => _api.InstallSelfUpdate();
    }
}