// <copyright company="SIX Networks GmbH" file="UpdateInstalledCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;

namespace SN.withSIX.Play.Applications.UseCases
{
    public class UpdateInstalledCommand : IAsyncRequest<Unit> {}

    public class UpdateInstalledCommandHandler : IAsyncRequestHandler<UpdateInstalledCommand, Unit>
    {
        public async Task<Unit> Handle(UpdateInstalledCommand request) {
            throw new NotImplementedException();
        }
    }
}