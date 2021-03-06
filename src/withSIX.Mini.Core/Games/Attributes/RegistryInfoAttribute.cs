// <copyright company="SIX Networks GmbH" file="RegistryInfoAttribute.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;

namespace withSIX.Mini.Core.Games.Attributes
{
    public class RegistryInfoAttribute : Attribute
    {
        public static readonly RegistryInfoAttribute Default = new NullRegistryInfo();
        protected RegistryInfoAttribute() {}

        public RegistryInfoAttribute(string path, string key) {
            if (!(!string.IsNullOrWhiteSpace(path))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(path)");
            if (key == null) throw new ArgumentNullException(nameof(key));
            Path = path;
            Key = key;
        }

        public string Path { get; }
        public string Key { get; }

        class NullRegistryInfo : RegistryInfoAttribute {}
    }
}