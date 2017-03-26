// <copyright company="SIX Networks GmbH" file="Arma3GameSettingsApiModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Mini.Plugin.Arma.Models;

namespace withSIX.Mini.Plugin.Arma.ApiModels
{
    public class Arma3GameSettingsApiModel : Arma2OaGameSettingsApiModel
    {
        public Platform Platform { get; set; }
    }
}
