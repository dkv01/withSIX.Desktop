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
        Task<List<CollectionModelWithLatestVersion>> Collections(Guid gameId, CsvList<Guid> ids,
            CancellationToken ct);

        Task<Games.ApiHashes> Hashes(Guid gameId, CancellationToken ct);
        Task<List<ModClientApiJson>> Mods(Guid gameId, string version, CancellationToken ct);
        Task<List<GroupContent>> GroupContent(Guid id, CancellationToken ct);
        Task<GroupAccess> GroupAccess(Guid id, CancellationToken ct);
        Task CreateStatusOverview(InstallStatusOverview stats, CancellationToken ct);
    }


    // https://github.com/paulcbetts/refit/issues/93
    // doesnt include the multiple &key=, so should adjust server then, if possible
    public class CsvList<T>
    {
        IEnumerable<T> values;

        // Unfortunately, you have to use a concrete type rather than IEnumerable<T> here
        public static implicit operator CsvList<T>(List<T> values) {
            return new CsvList<T> {values = values};
        }

        public override string ToString() {
            if (values == null)
                return null;
            return string.Join(",", values);
        }
    }

    // bad
    public class CsvList2<T>
    {
        string name;
        IEnumerable<T> values;

        public static CsvList2<T> Create(List<T> values, string name) {
            return new CsvList2<T> {values = values, name = name};
        }

        public override string ToString() {
            if (values == null)
                return null;
            return string.Join($"&ids=", values);
        }
    }
}