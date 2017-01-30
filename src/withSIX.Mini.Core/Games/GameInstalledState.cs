// <copyright company="SIX Networks GmbH" file="GameInstalledState.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using NDepend.Path;

namespace withSIX.Mini.Core.Games
{
    public class GameInstalledState
    {
        public static readonly GameInstalledState Default = new NotGameInstalledState();

        public GameInstalledState(IAbsoluteFilePath executable, IAbsoluteDirectoryPath directory, Version version = null) {
            if (executable == null) throw new ArgumentNullException(nameof(executable));
            if (directory == null) throw new ArgumentNullException(nameof(directory));

            Executable = executable;
            Directory = directory;
            Version = version;
        }

        protected GameInstalledState() {}
        public virtual bool IsInstalled => true;
        public IAbsoluteDirectoryPath Directory { get; }
        public Version Version { get; }
        public IAbsoluteFilePath Executable { get; }

        class NotGameInstalledState : GameInstalledState
        {
            public override bool IsInstalled => false;
        }
    }
}