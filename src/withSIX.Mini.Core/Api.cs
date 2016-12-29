// <copyright company="SIX Networks GmbH" file="Api.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Api.Models.Collections;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.v3;
using withSIX.Mini.Core.Social;

namespace withSIX.Mini.Core
{
    public interface IW6Api
    {
        Task<List<CollectionModelWithLatestVersion>> Collections(Guid gameId, List<Guid> ids, CancellationToken ct, Guid userId);
        Task<Games.ApiHashes> Hashes(Guid gameId, CancellationToken ct);
        Task<List<ModClientApiJson>> Mods(Guid gameId, string version, CancellationToken ct);
        Task<List<GroupContent>> GroupContent(Guid id, CancellationToken ct);
        Task<GroupAccess> GroupAccess(Guid id, CancellationToken ct);
        Task CreateStatusOverview(InstallStatusOverview stats, CancellationToken ct);
    }
}