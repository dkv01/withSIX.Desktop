// <copyright company="SIX Networks GmbH" file="GameSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using NDepend.Path;

namespace SN.withSIX.Mini.Core.Games
{
    [DataContract]
    public abstract class GameSettings
    {
        protected GameSettings(GameStartupParameters startupParameters) {
            StartupParameters = startupParameters;
        }

        [DataMember]
        public GameStartupParameters StartupParameters { get; protected set; }
        [DataMember]
        public IAbsoluteDirectoryPath GameDirectory { get; set; }
        [DataMember]
        public IAbsoluteDirectoryPath RepoDirectory { get; set; }
        [DataMember]
        public bool? LaunchAsAdministrator { get; set; }
    }

    public interface IHavePackageDirectory
    {
        IAbsoluteDirectoryPath PackageDirectory { get; set; }
    }

    [DataContract]
    public abstract class GameSettingsWithConfigurablePackageDirectory : GameSettings, IHavePackageDirectory
    {
        protected GameSettingsWithConfigurablePackageDirectory(GameStartupParameters startupParameters)
            : base(startupParameters) {}

        [DataMember]
        public IAbsoluteDirectoryPath PackageDirectory { get; set; }
    }
}