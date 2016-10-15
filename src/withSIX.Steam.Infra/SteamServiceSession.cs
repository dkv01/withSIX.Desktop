// <copyright company="SIX Networks GmbH" file="SteamServiceSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Steam.Core.Requests;
using withSIX.Steam.Core.Services;

namespace withSIX.Steam.Infra
{
    public abstract class SteamServiceSession : ISteamServiceSession
    {
        public abstract Task Start(uint appId, Uri uri);

        public abstract Task<BatchResult> GetServers<T>(GetServers query, Action<List<T>> pageAction,
            CancellationToken ct) where T : ServerInfoModel;

        public abstract Task<BatchResult> GetServerAddresses(GetServerAddresses query,
            Action<List<IPEndPoint>> pageAction, CancellationToken ct);
    }

    public static class ApiSerializerSettings
    {
        // TODO: Consider implications for the actual S-IR system messages: http://stackoverflow.com/questions/37832165/signalr-net-core-camelcase-json-contract-resolver/39410434#39410434
        public static JsonSerializerSettings GetSettings()
            => new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto}.SetDefaultSettings();
    }
}