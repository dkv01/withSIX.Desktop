// <copyright company="SIX Networks GmbH" file="RegistryInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;

namespace withSIX.Play.Core.Games.Entities
{
    public class RegistryInfo
    {
        protected RegistryInfo() {}

        public RegistryInfo(string path, string key) {
            if (!(!string.IsNullOrWhiteSpace(path))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(path)");
            if (key == null) throw new ArgumentNullException(nameof(key));
            Path = path;
            Key = key;
        }

        public string Path { get; }
        public string Key { get; }
    }

    public class NullRegistryInfo : RegistryInfo {}
}