// <copyright company="SIX Networks GmbH" file="Cancel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Services;
using withSIX.Core.Logging;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Core.Games.Services;
using withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace withSIX.Mini.Applications.Features.Main
{
    [ApiUserAction]
    public class Pause : RequestBase, IVoidCommandBase, IHaveId<Guid>
    {
        public Pause(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    [ApiUserAction]
    public class CancelAll : RequestBase, IVoidCommandBase {}

    public class AbortCommandHandler : IAsyncRequestHandler<Pause>, IAsyncRequestHandler<CancelAll>
    {
        readonly IContentInstallationService _contentInstallation;

        public AbortCommandHandler(IContentInstallationService contentInstallation) {
            _contentInstallation = contentInstallation;
        }

        public async Task Handle(CancelAll request) {
            //await new ActionNotification(Guid.Empty, "Aborting", "", request.ClientId, request.RequestId).Raise() .ConfigureAwait(false);
            await _contentInstallation.Abort().ConfigureAwait(false);
            //            await new ActionNotification(Guid.Empty, "Aborted", "", request.ClientId, request.RequestId).Raise().ConfigureAwait(false);
            
        }

        public async Task Handle(Pause request) {
            //await new ActionNotification(request.Id, "Aborting", "", request.ClientId, request.RequestId).Raise().ConfigureAwait(false);
            try {
                await _contentInstallation.Abort(request.Id).ConfigureAwait(false);
            } catch (NotLockedException ex) {
                MainLog.Logger.Info($"The game with ID {request.Id} does not appear to be locked. (Already done?)");
            }
            //await new ActionNotification(request.Id, "Aborted", "", request.ClientId, request.RequestId).Raise().ConfigureAwait(false);
            
        }
    }
}