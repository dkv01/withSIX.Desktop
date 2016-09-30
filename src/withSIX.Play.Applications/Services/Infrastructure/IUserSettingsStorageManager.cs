// <copyright company="SIX Networks GmbH" file="IUserSettingsStorageManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Play.Core.Options;

namespace withSIX.Play.Applications.Services.Infrastructure
{
    public interface IUserSettingsStorageManager
    {
        Task<UserSettings> GetCurrent();
        Task Save(UserSettings settings);
    }
}