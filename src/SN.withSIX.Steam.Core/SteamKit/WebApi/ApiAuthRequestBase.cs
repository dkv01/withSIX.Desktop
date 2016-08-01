// <copyright company="SIX Networks GmbH" file="ApiAuthRequestBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Diagnostics.CodeAnalysis;

namespace SN.withSIX.Steam.Core.SteamKit.WebApi
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public abstract class ApiAuthRequestBase : ApiRequestBase
    {
        public ApiAuthRequestBase(string apiKey) {
            key = apiKey;
        }

        public string key { get; set; }
    }
}