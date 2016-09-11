// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Steam.Core;

namespace SN.withSIX.Mini.Applications
{
    public class Initializer : IInitializer
    {
        public async Task Initialize() {
            Game.SteamHelper = SteamHelper.Create(); // TODO: Move
        }

        public async Task Deinitialize() {}
    }
}