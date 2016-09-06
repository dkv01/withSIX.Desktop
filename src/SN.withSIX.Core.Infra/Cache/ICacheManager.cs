// <copyright company="SIX Networks GmbH" file="ICacheManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Akavache;

namespace SN.withSIX.Core.Infra.Cache
{
    public interface ICacheManager
    {
        Task Shutdown();
        Task Vacuum();
        Task VacuumIfNeeded(TimeSpan timeAgo);
        void RegisterCache(IBlobCache cache);
    }
}