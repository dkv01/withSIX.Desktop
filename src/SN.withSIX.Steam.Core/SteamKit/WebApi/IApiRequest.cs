// <copyright company="SIX Networks GmbH" file="IApiRequest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace SN.withSIX.Steam.Core.SteamKit.WebApi
{
    public interface IApiRequest
    {
        Uri ToUri();
    }
}