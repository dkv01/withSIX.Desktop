// <copyright company="SIX Networks GmbH" file="IContentApiHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.v2;

namespace withSIX.Play.Core.Games.Services.Infrastructure
{
    public interface IContentApiHandler
    {
        bool Loaded { get; }
        Task LoadFromDisk();
        List<T> GetList<T>();
        Task<bool> LoadFromApi();
        Task<bool> LoadFromApi(ApiHashes hashes);
    }
}