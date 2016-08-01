// <copyright company="SIX Networks GmbH" file="ApiRequestBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;
using SN.withSIX.Steam.Core.SteamKit.Utils;

namespace SN.withSIX.Steam.Core.SteamKit.WebApi
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public abstract class ApiRequestBase : IApiRequest
    {
        public EResponseFormatType format { get; set; }
        protected abstract Uri ApiUri { get; }

        public virtual Uri ToUri() {
            var steamQueryString = this.ToSteamQueryString();
            return new Uri(ApiUri + "?" + steamQueryString);
        }
    }
}