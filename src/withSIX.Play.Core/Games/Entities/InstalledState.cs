// <copyright company="SIX Networks GmbH" file="InstalledState.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using NDepend.Path;

namespace withSIX.Play.Core.Games.Entities
{
    public class InstalledState
    {
        public InstalledState(IAbsoluteFilePath executable, IAbsoluteFilePath launchExecutable,
            IAbsoluteDirectoryPath directory, IAbsoluteDirectoryPath workingDirectory, Version version = null,
            bool isClient = true) {
            if (executable == null) throw new ArgumentNullException(nameof(executable));
            if (launchExecutable == null) throw new ArgumentNullException(nameof(launchExecutable));
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            if (workingDirectory == null) throw new ArgumentNullException(nameof(workingDirectory));

            Executable = executable;
            LaunchExecutable = launchExecutable;
            Directory = directory;
            WorkingDirectory = workingDirectory;
            Version = version;
            IsClient = isClient;
        }

        protected InstalledState() {}
        public virtual bool IsInstalled => true;
        public bool IsClient { get; }
        public IAbsoluteDirectoryPath WorkingDirectory { get; }
        public IAbsoluteDirectoryPath Directory { get; }
        public Version Version { get; }
        public IAbsoluteFilePath Executable { get; }
        public IAbsoluteFilePath LaunchExecutable { get; }
    }

    public class NotInstalledState : InstalledState
    {
        public override bool IsInstalled => false;
    }
}