// <copyright company="SIX Networks GmbH" file="EResponseFormatType.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;

namespace SN.withSIX.Steam.Api.SteamKit.Utils
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum EResponseFormatType
    {
        json,
        vdf,
        [Obsolete("Valid XML is not always returned")] xml
    }
}