﻿// <copyright company="SIX Networks GmbH" file="SteamServiceSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Core;
using withSIX.Steam.Core.Requests;
using withSIX.Steam.Core.Services;

namespace withSIX.Steam.Infra
{
    public abstract class SteamServiceSession : ISteamServiceSession
    {
        public abstract Task Start(uint appId, Uri uri);
        [Obsolete]
        public abstract Task<BatchResult> GetServerInfo<T>(GetServerInfo query, Action<List<T>> pageAction, CancellationToken ct);
        public abstract Task<BatchResult> GetServers<T>(GetServers query, Action<List<T>> pageAction, CancellationToken ct);
        public abstract Task<BatchResult> GetServerAddresses(GetServerAddresses query, Action<List<IPEndPoint>> pageAction, CancellationToken ct);
    }
}