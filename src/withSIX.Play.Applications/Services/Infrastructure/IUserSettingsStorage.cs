// <copyright company="SIX Networks GmbH" file="IUserSettingsStorage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using withSIX.Play.Core.Options;

namespace withSIX.Play.Applications.Services.Infrastructure
{
    public interface IUserSettingsStorage
    {
        UserSettings TryLoadSettings();
        void Save();
        Task SaveNow();
        IEnumerable<Assembly> GetDiscoverableAssemblies();
    }
}