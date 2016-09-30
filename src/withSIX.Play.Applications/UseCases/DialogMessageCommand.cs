// <copyright company="SIX Networks GmbH" file="DialogMessageCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;

namespace withSIX.Play.Applications.UseCases
{
    public class DialogMessageCommand : IAsyncRequest<Unit>
    {
        public string Message { get; set; }
        public string Title { get; set; }
    }

    public class DialogMessageCommandHandler : IAsyncRequestHandler<DialogMessageCommand, Unit>
    {
        readonly IDialogManager _dialogManager;

        public DialogMessageCommandHandler(IDialogManager dialogManager) {
            _dialogManager = dialogManager;
        }

        public async Task<Unit> Handle(DialogMessageCommand request) {
            await _dialogManager.MessageBox(new MessageBoxDialogParams(request.Message, request.Title)).ConfigureAwait(false);
            return Unit.Value;
        }
    }
}