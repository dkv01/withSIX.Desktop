// <copyright company="SIX Networks GmbH" file="ServiceRegistry.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Mini.Applications.Services;
using SteamLayerWrap;

namespace withSIX.Steam.Plugin.Arma
{
    public class ArmaServiceRegistry : ServiceRegistry
    {
        public ArmaServiceRegistry(IRegisterServices api) : base(api) {
            RegisterSingleton<ISteamAPIWrap, SteamAPIWrap>();
        }
    }
}