// <copyright company="SIX Networks GmbH" file="DeleteProfileCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using MediatR;
using withSIX.Play.Core.Options;

namespace withSIX.Play.Applications.UseCases.Profiles
{
    public class DeleteProfileCommand : IRequest<Unit>
    {
        public DeleteProfileCommand(Guid guid) {
            Contract.Requires<ArgumentNullException>(guid != Guid.Empty);

            Guid = guid;
        }

        public Guid Guid { get; }
    }

    
    public class DeleteProfileCommandHandler : IRequestHandler<DeleteProfileCommand, Unit>
    {
        readonly UserSettings _settings;

        public DeleteProfileCommandHandler(UserSettings settings) {
            _settings = settings;
        }

        public Unit Handle(DeleteProfileCommand request) {
            _settings.GameOptions.GameSettingsController.DeleteProfile(request.Guid);
            return default(Unit);
        }
    }
}