// <copyright company="SIX Networks GmbH" file="GameInstalledState.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using NDepend.Path;

namespace SN.withSIX.Mini.Core.Games
{
    public class GameInstalledState
    {
        public static readonly GameInstalledState Default = new NotGameInstalledState();

        public GameInstalledState(IAbsoluteFilePath executable, IAbsoluteDirectoryPath directory, Version version = null) {
            Contract.Requires<ArgumentNullException>(executable != null);
            Contract.Requires<ArgumentNullException>(directory != null);

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