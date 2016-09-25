// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Applications
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