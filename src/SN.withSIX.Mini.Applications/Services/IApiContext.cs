// <copyright company="SIX Networks GmbH" file="IApiContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using withSIX.Api.Models.Content.v3;
using ApiHashes = SN.withSIX.Mini.Core.Games.ApiHashes;

namespace SN.withSIX.Mini.Applications.Services
{
    public interface IApiContext
    {
        Task<List<ModClientApiJsonV3WithGameId>> GetMods(Guid gameId, string version);

        Task<ApiHashes> GetHashes(Guid gameId);
    }

    public static class ApiExtensions
    {
        public static string GetVersion(this ModClientApiJson This) => This.LatestStableVersion ?? This.Version;
    }

    public class ModClientApiJsonV3WithGameId : ModClientApiJson, IHaveId<Guid>
    {
        private Guid _gameId;
        public Guid GameId
        {
            get { return _gameId; }
            set
            {
                if (value == Guid.Empty)
                    throw new ArgumentException(nameof(value));
                _gameId = value;
            }
        }
        // Only used for BWC
        public List<string> Tags { get; set; }

        public new string Name
        {
            get { return null; }
            set { }
        }
        public string GetVersion() => LatestStableVersion ?? Version;
    }
}