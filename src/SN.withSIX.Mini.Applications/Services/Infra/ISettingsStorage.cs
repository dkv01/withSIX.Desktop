// <copyright company="SIX Networks GmbH" file="ISettingsStorage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Applications.Models;

namespace SN.withSIX.Mini.Applications.Services.Infra
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