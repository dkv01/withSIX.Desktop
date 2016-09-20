// <copyright company="SIX Networks GmbH" file="IServerQuery.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;

namespace GameServerQuery
{
    public interface IServerQuery
    {
        Task UpdateAsync();
    }
}