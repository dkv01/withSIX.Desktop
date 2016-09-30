// <copyright company="SIX Networks GmbH" file="ChatMessageRecievedCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;

namespace withSIX.Play.Applications.UseCases
{
    public class ChatMessageRecievedCommand : IAsyncRequest<Unit> {}

    public class ChatMessageRecievedCommandHandler : IAsyncRequestHandler<ChatMessageRecievedCommand, Unit>
    {
        public async Task<Unit> Handle(ChatMessageRecievedCommand request) {
            throw new NotImplementedException();
        }
    }
}