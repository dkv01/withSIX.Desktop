// <copyright company="SIX Networks GmbH" file="LaunchGameInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using NDepend.Path;

namespace withSIX.Play.Core.Games.Services
{
    public class LaunchGameInfo : LaunchGameInfoBase
    {
        public LaunchGameInfo(IAbsoluteFilePath launchExecutable, IAbsoluteFilePath expectedExecutable,
            IAbsoluteDirectoryPath workingDirectory,
            IEnumerable<string> startupParameters)
            : base(launchExecutable, expectedExecutable, workingDirectory, startupParameters) {}

        public bool InjectSteam { get; set; }
    }

    public abstract class LaunchGameInfoBase
    {
        protected LaunchGameInfoBase(IAbsoluteFilePath launchExecutable, IAbsoluteFilePath expectedExecutable,
            IAbsoluteDirectoryPath workingDirectory,
            IEnumerable<string> startupParameters) {
            if (launchExecutable == null) throw new ArgumentNullException(nameof(launchExecutable));
            if (workingDirectory == null) throw new ArgumentNullException(nameof(workingDirectory));
            LaunchExecutable = launchExecutable;
            ExpectedExecutable = expectedExecutable;
            WorkingDirectory = workingDirectory;
            StartupParameters = startupParameters;
        }

        public IAbsoluteFilePath LaunchExecutable { get; }
        public IAbsoluteDirectoryPath WorkingDirectory { get; }
        public IEnumerable<string> StartupParameters { get; }
        public ProcessPriorityClass Priority { get; set; }
        public int[] Affinity { get; set; }
        public bool LaunchAsAdministrator { get; set; }
        public bool WaitForExit { get; set; }
        public IAbsoluteFilePath ExpectedExecutable { get; }
    }

    public class LaunchGameWithJavaInfo : LaunchGameInfoBase
    {
        public LaunchGameWithJavaInfo(IAbsoluteFilePath launchExecutable, IAbsoluteFilePath expectedExecutable,
            IAbsoluteDirectoryPath workingDirectory,
            IEnumerable<string> startupParameters)
            : base(launchExecutable, expectedExecutable, workingDirectory, startupParameters) {}
    }

    public class LaunchGameWithSteamInfo : LaunchGameInfoBase
    {
        public LaunchGameWithSteamInfo(IAbsoluteFilePath launchExecutable, IAbsoluteFilePath expectedExecutable,
            IAbsoluteDirectoryPath workingDirectory,
            IEnumerable<string> startupParameters)
            : base(launchExecutable, expectedExecutable, workingDirectory, startupParameters) {}

        public int SteamAppId { get; set; }
        public bool SteamDRM { get; set; }
    }

    public class LaunchGameWithSteamLegacyInfo : LaunchGameWithSteamInfo
    {
        public LaunchGameWithSteamLegacyInfo(IAbsoluteFilePath launchExecutable, IAbsoluteFilePath expectedExecutable,
            IAbsoluteDirectoryPath workingDirectory,
            IEnumerable<string> startupParameters)
            : base(launchExecutable, expectedExecutable, workingDirectory, startupParameters) {}
    }
}