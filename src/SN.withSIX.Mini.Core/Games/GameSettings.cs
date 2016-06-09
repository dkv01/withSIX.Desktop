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
        [DataMember, Obsolete("old workaround")]
        protected string RepoDirectoryInternal { get; set; }
        [DataMember, Obsolete("old workaround")]
        protected string GameDirectoryInternal { get; set; }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context) {
            if (RepoDirectoryInternal != null) {
                RepoDirectory = RepoDirectoryInternal.ToAbsoluteDirectoryPath();
                RepoDirectoryInternal = null;
            }
            if (GameDirectoryInternal != null) {
                GameDirectory = GameDirectoryInternal.ToAbsoluteDirectoryPath();
                GameDirectoryInternal = null;
            }
        }
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

        [DataMember, Obsolete("old workaround")]
        protected string PackageDirectoryInternal { get; set; }
        [DataMember]
        public IAbsoluteDirectoryPath PackageDirectory { get; set; }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context) {
            if (PackageDirectoryInternal != null) {
                PackageDirectory = PackageDirectoryInternal.ToAbsoluteDirectoryPath();
                PackageDirectoryInternal = null;
            }
        }
    }
}