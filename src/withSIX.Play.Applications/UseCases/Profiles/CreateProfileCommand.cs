// <copyright company="SIX Networks GmbH" file="CreateProfileCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using MediatR;

using withSIX.Play.Applications.Services.Infrastructure;
using withSIX.Play.Core.Options;

namespace withSIX.Play.Applications.UseCases.Profiles
{
    public class CreateProfileCommand : IAsyncRequest<Guid>
    {
        public CreateProfileCommand(string name, string color, Guid guid) {
            if (!(!string.IsNullOrWhiteSpace(name))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(name)");
            if (!(!string.IsNullOrWhiteSpace(color))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(color)");
            if (!(guid != Guid.Empty)) throw new ArgumentNullException("guid != Guid.Empty");

            Name = name;
            Color = color;
            Guid = guid;
        }

        public string Name { get; }
        public string Color { get; }
        public Guid Guid { get; }
    }

    
    public class CreateProfileCommandHandler : IAsyncRequestHandler<CreateProfileCommand, Guid>
    {
        readonly UserSettings _settings;
        readonly IUserSettingsStorage _storage;

        public CreateProfileCommandHandler(UserSettings settings, IUserSettingsStorage storage) {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _settings = settings;
            _storage = storage;
        }

        public async Task<Guid> Handle(CreateProfileCommand request) {
            var newProfile = _settings.GameOptions.GameSettingsController.CreateProfile(request.Name, request.Color,
                request.Guid);

            await _storage.SaveNow().ConfigureAwait(false);

            return newProfile.Id;
        }
    }
}