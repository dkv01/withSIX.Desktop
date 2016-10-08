// <copyright company="SIX Networks GmbH" file="SteamHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Core;
using withSIX.Steam.Core.Requests;

namespace withSIX.Steam.Core.Services
{
    public class ServersInfo<T>
    {
        public List<T> Servers { get; set; }
    }

    public interface ISteamHelperService
    {
        Task<ServersInfo<T>> GetServers<T>(uint appId, GetServerInfo query, CancellationToken ct);
    }

    public interface ISteamHelperRunner
    {
        Task RunHelperInternal(CancellationToken cancelToken, IEnumerable<string> parameters,
            Action<Process, string> standardOutputAction, Action<Process, string> standardErrorAction);

        IEnumerable<string> GetHelperParameters(string command, uint appId, params string[] options);
    }

    public interface ISteamServiceSession
    {
        Task Start(uint appId, Uri uri);
        Task<BatchResult> GetServerInfo<T>(GetServerInfo query, Action<List<T>> pageAction, CancellationToken ct);
        Task<BatchResult> GetServers<T>(GetServers query, Action<List<T>> pageAction, CancellationToken ct);
    }
}
