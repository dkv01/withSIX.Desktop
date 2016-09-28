// <copyright company="SIX Networks GmbH" file="Cancel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Applications.Attributes;
using SN.withSIX.Mini.Core.Games.Services;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Api.Models.Content.v3;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    [ApiUserAction]
    public class Pause : RequestBase, IAsyncVoidCommandBase, IHaveId<Guid>
    {
        public Pause(Guid id) {
            Id = id;
        }

        public Guid Id { get; }
    }

    [ApiUserAction]
    public class CancelAll : RequestBase, IAsyncVoidCommandBase {}

    public class AbortCommandHandler : IAsyncVoidCommandHandler<Pause>, IAsyncVoidCommandHandler<CancelAll>
    {
        readonly IContentInstallationService _contentInstallation;

        public AbortCommandHandler(IContentInstallationService contentInstallation) {
            _contentInstallation = contentInstallation;
        }

        public async Task<Unit> Handle(CancelAll request) {
            //await new ActionNotification(Guid.Empty, "Aborting", "", request.ClientId, request.RequestId).Raise() .ConfigureAwait(false);
            await _contentInstallation.Abort().ConfigureAwait(false);
            //            await new ActionNotification(Guid.Empty, "Aborted", "", request.ClientId, request.RequestId).Raise().ConfigureAwait(false);
            return Unit.Value;
        }

        public async Task<Unit> Handle(Pause request) {
            //await new ActionNotification(request.Id, "Aborting", "", request.ClientId, request.RequestId).Raise().ConfigureAwait(false);
            try {
                await _contentInstallation.Abort(request.Id).ConfigureAwait(false);
            } catch (NotLockedException ex) {
                MainLog.Logger.Info($"The game with ID {request.Id} does not appear to be locked. (Already done?)");
            }
            //await new ActionNotification(request.Id, "Aborted", "", request.ClientId, request.RequestId).Raise().ConfigureAwait(false);
            return Unit.Value;
        }
    }
}