// <copyright company="SIX Networks GmbH" file="GameSettingsFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using FluentValidation;
using withSIX.Api.Models.Content.v3;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Factories
{
    public interface IGameSettingsViewModelFactory
    {
        IGameSettingsApiModel CreateApiModel(Game game);
    }

    public interface IGameSettingsApiModel : IHaveId<Guid>
    {
        string GameDirectory { get; set; }
        string RepoDirectory { get; set; }
        string StartupLine { get; set; }
    }

    public abstract class GameSettingsApiModel : IGameSettingsApiModel
    {
        public bool? LaunchAsAdministrator { get; set; }
        public Guid Id { get; set; }
        public string GameDirectory { get; set; }
        public string RepoDirectory { get; set; }
        public string StartupLine { get; set; }
    }

    public class GameSettingsApiModelValidator : AbstractValidator<GameSettingsApiModel>
    {
        public GameSettingsApiModelValidator(ValidPathValidator pathValidator) {
            RuleFor(x => x.StartupLine).NotNull();
            RuleFor(x => x.GameDirectory)
                .SetValidator(pathValidator);
            RuleFor(x => x.RepoDirectory)
                .SetValidator(pathValidator);
        }
    }

    public abstract class GameSettingsWithConfigurablePackageApiModel : GameSettingsApiModel
    {
        public string PackageDirectory { get; set; }
    }

    public class GameSettingsWithConfigurablePackageApiModelValidator :
        AbstractValidator<GameSettingsWithConfigurablePackageApiModel>
    {
        public GameSettingsWithConfigurablePackageApiModelValidator(ValidPathValidator pathValidator) {
            RuleFor(x => x.PackageDirectory)
                .SetValidator(pathValidator);
        }
    }
}