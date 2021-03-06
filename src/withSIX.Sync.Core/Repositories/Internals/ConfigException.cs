// <copyright company="SIX Networks GmbH" file="ConfigException.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Sync.Core.Repositories.Internals
{
    public class ConfigException : Exception
    {
        public ConfigException() {}
        public ConfigException(string message) : base(message) {}
    }
}