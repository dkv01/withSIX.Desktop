// <copyright company="SIX Networks GmbH" file="Exceptions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Core;

namespace SN.withSIX.Steam.Core
{
    public class SteamInitializationException : DidNotStartException
    {
        public SteamInitializationException(string message) : base(message) {}
    }

    public class SteamNotFoundException : DidNotStartException
    {
        public SteamNotFoundException(string message) : base(message) {}
    }
}