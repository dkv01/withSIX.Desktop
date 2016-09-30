// <copyright company="SIX Networks GmbH" file="IShortcutCreation.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using withSIX.Play.Core.Games.Services.GameLauncher;

namespace withSIX.Play.Core.Games.Entities.RealVirtuality
{
    public interface IShortcutCreation
    {
        Task<IReadOnlyCollection<string>> ShortcutLaunchParameters(IGameLauncherFactory factory, string identifier);
    }
}