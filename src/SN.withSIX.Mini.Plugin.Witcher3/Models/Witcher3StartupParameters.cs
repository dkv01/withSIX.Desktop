// <copyright company="SIX Networks GmbH" file="Witcher3StartupParameters.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Plugin.Witcher3.Models
{
    [DataContract]
    public class Witcher3StartupParameters : GameStartupParameters
    {
        static readonly Regex propertyRegex = new Regex(
            @"(?<property>(?<![\w])[-](?<name>\w+) (?<value>(?=[^-])[^ ]+))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex switchRegex = new Regex(@"(?<switch>(?<![\w])[-](?<name>[^ ]+)(?![ ][\w]))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public Witcher3StartupParameters(string[] defaultParameters) : base(defaultParameters) {}
        public Witcher3StartupParameters() {}

        protected override IEnumerable<string> BuildSwitches() => SwitchStorage.Select(BuildSwitch);

        static string BuildSwitch(string @switch) => $"-{@switch.ToLower()}";

        protected override IEnumerable<string> BuildParameters() => ParameterStorage.Select(BuildParameter);

        static string BuildParameter(KeyValuePair<string, string> setting) =>
            $"-{setting.Key.ToLower()} {setting.Value}";

        protected override void ParseInputString(string input) {
            var properties = propertyRegex.Matches(input);
            foreach (Match p in properties) {
                input = input.Replace(p.Groups[0].Value, string.Empty);
                SetPropertyOrDefault(CutdownOnTrailingBackslashes(p.Groups["value"].Value), p.Groups["name"].Value, true);
            }
            var switches = switchRegex.Matches(input);
            foreach (Match s in switches)
                SetSwitchOrDefault(true, s.Groups["name"].Value, true);
        }
    }
}