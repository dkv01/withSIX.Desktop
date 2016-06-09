// <copyright company="SIX Networks GmbH" file="GameInstalledState.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using NDepend.Path;

namespace SN.withSIX.Mini.Core.Games
{
    public class GameInstalledState
    {
        public static readonly GameInstalledState Default = new NotGameInstalledState();

        public GameInstalledState(IAbsoluteFilePath executable, IAbsoluteFilePath launchExecutable,
            IAbsoluteDirectoryPath directory, IAbsoluteDirectoryPath workingDirectory, Version version = null,
            bool isClient = true) {
            /*
        Contract.Requires<ArgumentNullException>(executable != null);
        Contract.Requires<ArgumentNullException>(launchExecutable != null);
        Contract.Requires<ArgumentNullException>(directory != null);
        Contract.Requires<ArgumentNullException>(workingDirectory != null);
        */

            Executable = executable;
            LaunchExecutable = launchExecutable;
            Directory = directory;
            WorkingDirectory = workingDirectory;
            Version = version;
            IsClient = isClient;
        }

        protected GameInstalledState() {}
        public virtual bool IsInstalled => true;
        public bool IsClient { get; }
        public IAbsoluteDirectoryPath WorkingDirectory { get; }
        public IAbsoluteDirectoryPath Directory { get; }
        public Version Version { get; }
        public IAbsoluteFilePath Executable { get; }
        public IAbsoluteFilePath LaunchExecutable { get; }

        class NotGameInstalledState : GameInstalledState
        {
            public override bool IsInstalled => false;
        }
    }
}