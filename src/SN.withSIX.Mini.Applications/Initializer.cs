// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Applications.Services;

namespace withSIX.Mini.Applications
{
    public class Initializer : IInitializer
    {
        public Task Initialize() {
            CloseRunningSteamHelpers();
            return TaskExt.Default;
        }

        public Task Deinitialize() {
            CloseRunningSteamHelpers();
            return TaskExt.Default;
        }

        private static void CloseRunningSteamHelpers() {
            Tools.ProcessManager.Management.KillByName("withSIX.SteamHelper.exe", null, true);
        }
    }
}