// <copyright company="SIX Networks GmbH" file="SwitchProfileCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using MediatR;

using withSIX.Play.Core.Options;

namespace withSIX.Play.Applications.UseCases.Profiles
{
    public class SwitchProfileCommand : IRequest<Unit>
    {
        public SwitchProfileCommand(Guid guid) {
            if (!(guid != Guid.Empty)) throw new ArgumentNullException("guid != Guid.Empty");

            Guid = guid;
        }

        public Guid Guid { get; }
    }

    public class SwitchProfileByNameCommand : IRequest<Unit>
    {
        public SwitchProfileByNameCommand(string name) {
            if (!(!string.IsNullOrWhiteSpace(name))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(name)");
            Name = name;
        }

        public string Name { get; }
    }


    
    public class SwitchProfileCommandHandler : IRequestHandler<SwitchProfileCommand, Unit>,
        IRequestHandler<SwitchProfileByNameCommand, Unit>
    {
        readonly UserSettings _settings;

        public SwitchProfileCommandHandler(UserSettings settings) {
            _settings = settings;
        }

        public Unit Handle(SwitchProfileByNameCommand request) {
            _settings.GameOptions.GameSettingsController.ActivateProfile(request.Name);
            return default(Unit);
        }

        public Unit Handle(SwitchProfileCommand request) {
            _settings.GameOptions.GameSettingsController.ActivateProfile(request.Guid);
            return default(Unit);
        }
    }
}