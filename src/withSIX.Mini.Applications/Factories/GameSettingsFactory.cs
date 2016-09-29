// <copyright company="SIX Networks GmbH" file="GameSettingsFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Api.Models.Content.v3;
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

    public abstract class GameSettingsWithConfigurablePackageApiModel : GameSettingsApiModel
    {
        public string PackageDirectory { get; set; }
    }
}