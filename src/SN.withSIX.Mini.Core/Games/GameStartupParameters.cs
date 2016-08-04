// <copyright company="SIX Networks GmbH" file="GameStartupParameters.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Core.Games
{
    [DataContract]
    public abstract class GameStartupParameters : PropertyChangedBase
    {
        string _startupLine;

        protected GameStartupParameters(string[] defaultParameters) {
            DefaultParams = defaultParameters.CombineParameters();
            Parse(DefaultParams, true);
        }

        protected GameStartupParameters() {}
        [DataMember]
        public Dictionary<string, string> ParameterStorage { get; protected set; } = new Dictionary<string, string>();
        [DataMember]
        public IList<string> SwitchStorage { get; protected set; } = new List<string>();

        private string DefaultParams { get; } = string.Empty;

        [Browsable(false)]
        public string StartupLine
        {
            get { return _startupLine; }
            set { Parse(value); }
        }

        public virtual IEnumerable<string> Get() => BuildParameters().Concat(BuildSwitches());

        protected abstract IEnumerable<string> BuildSwitches();
        protected abstract IEnumerable<string> BuildParameters();

        protected string GetPropertyOrDefault([CallerMemberName] string key = null) {
            key = key.ToLower();
            return ParameterStorage.ContainsKey(key) ? ParameterStorage[key] : null;
        }

        protected bool GetSwitchOrDefault([CallerMemberName] string key = null) => SwitchStorage.Contains(key.ToLower());

        protected void SetSwitchOrDefault(bool value, [CallerMemberName] string key = null, bool silent = false) {
            if (value)
                SwitchOn(key, silent);
            else
                SwitchOff(key, silent);
        }

        private void SwitchOn(string key, bool silent) {
            var lowerKey = key.ToLower();
            if (SwitchStorage.Contains(lowerKey))
                return;
            SwitchStorage.Add(lowerKey);
            if (!silent)
                Refresh(key);
        }

        private void SwitchOff(string key, bool silent) {
            var lowerKey = key.ToLower();
            if (!SwitchStorage.Contains(lowerKey))
                return;
            SwitchStorage.Remove(lowerKey);
            if (!silent)
                Refresh(key);
        }

        private void Refresh(string key) {
            OnPropertyChanged(key);
            UpdateStartupLine();
        }


        protected void SetPropertyOrDefault(string value, [CallerMemberName] string key = null, bool silent = false) {
            var lowerKey = key.ToLower();

            var hasKey = ParameterStorage.ContainsKey(lowerKey);
            if (string.IsNullOrWhiteSpace(value)) {
                if (!hasKey)
                    return;
                ParameterStorage.Remove(lowerKey);
                if (!silent)
                    Refresh(key);
                return;
            }

            if (hasKey) {
                if (ParameterStorage[lowerKey] == value)
                    return;
                ParameterStorage[lowerKey] = value;
                if (!silent)
                    Refresh(key);
                return;
            }

            ParameterStorage.Add(lowerKey, value);
            if (!silent)
                Refresh(key);
        }

        internal void Parse(string input, bool silent = false) {
            ParameterStorage = new Dictionary<string, string>();
            SwitchStorage = new List<string>();

            ParseInputString(input);

            UpdateStartupLine();
            if (!silent)
                Refresh();
        }

        protected abstract void ParseInputString(string input);

        protected static string CutdownOnTrailingBackslashes(string value) {
            var endsWithQuote = value.EndsWith("\"");

            if (endsWithQuote)
                value = value.Substring(0, value.Length - 1);

            var trail = endsWithQuote ? "\"" : null;
            if (value.EndsWith("\\"))
                return value.TrimEnd('\\') + "\\" + trail;
            return value + trail;
        }

        void UpdateStartupLine() {
            _startupLine = Get().CombineParameters();
            OnPropertyChanged("StartupLine");
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context) {
            UpdateStartupLine();
        }
    }
}