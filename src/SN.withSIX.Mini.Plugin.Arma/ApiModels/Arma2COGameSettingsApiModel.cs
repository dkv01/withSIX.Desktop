// <copyright company="SIX Networks GmbH" file="Arma2COGameSettingsApiModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Mini.Plugin.Arma.ApiModels
{
    public class Arma2COGameSettingsApiModel : Arma2OaGameSettingsApiModel {}

    public class Arma2OaGameSettingsApiModel : RealVirtualityGameSettingsApiModel
    {
        public bool LaunchThroughBattlEye { get; set; }
        public bool LaunchAsDedicatedServer { get; set; }
    }

    public class Arma2GameSettingsApiModel : RealVirtualityGameSettingsApiModel {}


    public class Arma1GameSettingsApiModel : RealVirtualityGameSettingsApiModel {}
}