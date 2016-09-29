// <copyright company="SIX Networks GmbH" file="CIV5Game.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Plugin.Firaxis
{
    public class CIV5Game : BasicSteamGame
    {
        public CIV5Game(Guid id, GameSettings settings) : base(id, settings) {}

        protected override Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            throw new NotImplementedException();
        }

        protected override Task InstallMod(IModContent mod) {
            throw new NotImplementedException();
        }

        protected override Task UninstallMod(IModContent mod) {
            throw new NotImplementedException();
        }
    }
}