// <copyright company="SIX Networks GmbH" file="ISettingsStorage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Core.Services.Infrastructure;
using withSIX.Mini.Applications.Models;

namespace withSIX.Mini.Applications.Services.Infra
{
    public interface ISettingsStorageReadOnly
    {
        Task<Settings> GetSettings();
    }

    public interface ISettingsStorage : ISettingsStorageReadOnly
    {
        IDomainEventHandler DomainEventHandler { get; }
        Task<int> SaveChanges();
    }
}