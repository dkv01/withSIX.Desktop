// <copyright company="SIX Networks GmbH" file="SULaunchGameArgumentsBuilder.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using NDepend.Path;
using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Play.Core.Games.Services;
using withSIX.Api.Models.Extensions;

namespace withSIX.Play.Infra.Data.Services
{
    abstract class SULaunchGameArgumentsBuilder
    {
        protected LaunchGameInfoBase Spec { get; }

        protected SULaunchGameArgumentsBuilder(LaunchGameInfoBase spec) {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (!(spec.LaunchExecutable != null)) throw new ArgumentNullException("spec.LaunchExecutable != null");
            if (!(spec.WorkingDirectory != null)) throw new ArgumentNullException("spec.WorkingDirectory != null");
            if (!(spec.StartupParameters != null)) throw new ArgumentNullException("spec.StartupParameters != null");
            Spec = spec;
        }

        protected string GetStartupParameters() => "--arguments=" + Spec.StartupParameters.CombineParameters();

        public virtual IEnumerable<string> Build() => new[] {
                UpdaterCommands.LaunchGame,
                "--gamePath=" + Spec.LaunchExecutable,
                "--workingDirectory=" + Spec.WorkingDirectory,
                GetPriority(),
                GetAffinity(),
                //GetRealExe(),
                GetStartupParameters()
            }.Where(x => x != null);

        protected string GetAffinity() => Spec.Affinity != null && Spec.Affinity.Any()
    ? "--affinity=" + string.Join(",", Spec.Affinity)
    : null;

        protected string GetPriority() => "--priority=" + Spec.Priority;

        protected string GetRealExe() {
            if (Spec.LaunchExecutable != Spec.ExpectedExecutable)
                return "--realExe=" + Spec.ExpectedExecutable;
            return null;
        }
    }

    class SULaunchDefaultGameArgumentsBuilder : SULaunchGameArgumentsBuilder
    {
        public SULaunchDefaultGameArgumentsBuilder(LaunchGameInfo spec) : base(spec) {}

        public override IEnumerable<string> Build() => new[] {
                UpdaterCommands.LaunchGame,
                "--gamePath=" + Spec.LaunchExecutable,
                "--workingDirectory=" + Spec.WorkingDirectory,
                GetPriority(),
                GetAffinity(),
                //GetRealExe(),
                GetStartupParameters()
            }.Where(x => x != null);
    }

    class SULaunchGameJavaArgumentsBuilder : SULaunchGameArgumentsBuilder
    {
        readonly IAbsoluteDirectoryPath _javaPath;

        public SULaunchGameJavaArgumentsBuilder(LaunchGameWithJavaInfo spec, IAbsoluteDirectoryPath javaPath)
            : base(spec) {
            if (javaPath == null) throw new ArgumentNullException(nameof(javaPath));
            _javaPath = javaPath;
        }

        public override IEnumerable<string> Build() {
            throw new NotImplementedException();
        }
    }

    class SULaunchGameSteamArgumentsBuilder : SULaunchGameArgumentsBuilder
    {
        readonly LaunchGameWithSteamInfo _spec;
        readonly IAbsoluteDirectoryPath _steamPath;

        public SULaunchGameSteamArgumentsBuilder(LaunchGameWithSteamInfo spec, IAbsoluteDirectoryPath steamPath)
            : base(spec) {
            if (steamPath == null) throw new ArgumentNullException(nameof(steamPath));
            if (!(spec.SteamAppId != -1)) throw new ArgumentNullException("spec.SteamAppId != -1");
            _steamPath = steamPath;
            _spec = spec;
        }

        protected string GetSteamAppId() => "--steamID=" + _spec.SteamAppId;

        protected string GetSteamPath() => "--steamPath=" + _steamPath;

        public override IEnumerable<string> Build() => new[] {
                UpdaterCommands.LaunchGame,
                "--gamePath=" + Spec.LaunchExecutable,
                "--workingDirectory=" + Spec.WorkingDirectory,
                GetSteamDRM(),
                GetSteamAppId(),
                GetSteamPath(),
                GetPriority(),
                GetAffinity(),
                //GetRealExe(),
                GetStartupParameters()
            }.Where(x => x != null);

        string GetSteamDRM() => _spec.SteamDRM ? "--steamDRM" : null;
    }

    class SULaunchGameSteamLegacyArgumentsBuilder : SULaunchGameSteamArgumentsBuilder
    {
        public SULaunchGameSteamLegacyArgumentsBuilder(LaunchGameWithSteamLegacyInfo spec,
            IAbsoluteDirectoryPath steamPath)
            : base(spec, steamPath) {}

        public override IEnumerable<string> Build() => new[] {
                UpdaterCommands.LaunchGame,
                "--gamePath=" + Spec.LaunchExecutable,
                "--workingDirectory=" + Spec.WorkingDirectory,
                "--legacyLaunch",
                GetSteamAppId(),
                GetSteamPath(),
                GetPriority(),
                GetAffinity(),
                //GetRealExe(),
                GetStartupParameters()
            }.Where(x => x != null);
    }
}