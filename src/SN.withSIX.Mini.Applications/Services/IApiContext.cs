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
        Task<List<ModClientApiJsonV3WithGameId>> GetMods(Guid gameId, string version,
            IEnumerable<Guid> desiredContent = null);

        Task<ApiHashes> GetHashes(Guid gameId);
    }

    public class ModClientApiJsonV3WithGameId : ModClientApiJson
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
        public string GetVersion() => LatestStableVersion ?? Version;
    }
}