// <copyright company="SIX Networks GmbH" file="FriendServerChangedCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;

namespace withSIX.Play.Applications.UseCases
{
    public class FriendServerChangedCommand : IAsyncRequest<Unit> {}

    public class FriendServerChangedCommandHandler : IAsyncRequestHandler<FriendServerChangedCommand, Unit>
    {
        public async Task<Unit> Handle(FriendServerChangedCommand request) {
            throw new NotImplementedException();
        }
    }
}