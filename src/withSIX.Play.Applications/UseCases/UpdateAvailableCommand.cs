// <copyright company="SIX Networks GmbH" file="UpdateAvailableCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Play.Applications.Services;

namespace SN.withSIX.Play.Applications.UseCases
{
    public class UpdateAvailableCommand : IAsyncRequest<Unit> {}

    public class UpdateAvailableCommandHandler :
        IAsyncRequestHandler<UpdateAvailableCommand, Unit>
    {
        readonly ISoftwareUpdate _softwareUpdate;

        public UpdateAvailableCommandHandler(ISoftwareUpdate softwareUpdate) {
            _softwareUpdate = softwareUpdate;
        }

        public async Task<Unit> Handle(UpdateAvailableCommand request) {
            _softwareUpdate.UpdateAndExitIfNotBusy(false);
            return Unit.Value;
        }
    }
}