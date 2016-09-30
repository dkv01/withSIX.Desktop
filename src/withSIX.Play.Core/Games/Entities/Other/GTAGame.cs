// <copyright company="SIX Networks GmbH" file="GTAGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NDepend.Path;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Play.Core.Options.Entries;

namespace withSIX.Play.Core.Games.Entities.Other
{
    // ReSharper disable once InconsistentNaming
    public abstract class GTAGame : RockstarGame, ISupportModding
    {
        ContentPaths _modPaths;

        protected GTAGame(Guid id, GTAGameSettings settings) : base(id, settings) {
            Settings = settings;
        }

        public new GTAGameSettings Settings { get; }
        public ContentPaths PrimaryContentPath => ModPaths;
        public ContentPaths ModPaths
        {
            get { return _modPaths ?? (_modPaths = GetModPaths()); }
            private set { SetProperty(ref _modPaths, value); }
        }

        public bool SupportsContent(IMod mod) => mod.GameId == Id;

        public IEnumerable<LocalModsContainer> LocalModsContainers() {
            var installedState = InstalledState;

            if (!installedState.IsInstalled)
                return Enumerable.Empty<LocalModsContainer>();

            return new[] {GameLocalModsContainer()};
        }

        //TODO: Unsure if needed for GTA V
        public IEnumerable<IAbsolutePath> GetAdditionalLaunchMods() => Enumerable.Empty<IAbsolutePath>();

        public void UpdateModStates(IReadOnlyCollection<IMod> mods) {
            foreach (var m in mods)
                m.Controller.UpdateState(this);
        }

        protected abstract LocalModsContainer GameLocalModsContainer();

        ContentPaths GetModPaths() => InstalledState.IsInstalled
    ? new ContentPaths(GetModDirectory(), GetRepositoryDirectory())
    : new NullContentPaths();

        IAbsoluteDirectoryPath GetRepositoryDirectory() => Settings.RepositoryDirectory ?? GetModDirectory();

        IAbsoluteDirectoryPath GetModDirectory() => GetExecutable().ParentDirectoryPath;
    }

    public class GTAStartupParameters : GameStartupParameters
    {
        static readonly Regex propertyRegex = new Regex(
            @"(?<property>(?<![\w])[-](?<name>\w+) (?<value>(?=[^-])[^ ]+))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex switchRegex = new Regex(@"(?<switch>(?<![\w])[-](?<name>[^ ]+)(?![ ][\w]))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected GTAStartupParameters(params string[] defaultParameters) : base(defaultParameters) {}

        protected override IEnumerable<string> BuildSwitches() => SwitchStorage.Select(BuildSwitch);

        static string BuildSwitch(string @switch) => $"-{@switch.ToLower()}";

        protected override IEnumerable<string> BuildParameters() => ParameterStorage.Select(BuildParameter);

        static string BuildParameter(KeyValuePair<string, string> setting) =>
            $"-{setting.Key.ToLower()} {setting.Value}";

        protected override void ParseInputString(string input) {
            var properties = propertyRegex.Matches(input);
            foreach (Match p in properties) {
                input = input.Replace(p.Groups[0].Value, String.Empty);
                SetPropertyOrDefault(CutdownOnTrailingBackslashes(p.Groups["value"].Value), p.Groups["name"].Value, true);
            }
            var switches = switchRegex.Matches(input);
            foreach (Match s in switches)
                SetSwitchOrDefault(true, s.Groups["name"].Value, true);
        }
    }

    public class GTAGameSettings : GameSettings
    {
        public GTAGameSettings(Guid gameId, GTAStartupParameters sp, GameSettingsController controller)
            : base(gameId, sp, controller) {}

        public IAbsoluteDirectoryPath RepositoryDirectory
        {
            get { return GetValue<string>().ToAbsoluteDirectoryPathNullSafe(); }
            set { SetValue(value == null ? null : value.ToString()); }
        }
    }
}