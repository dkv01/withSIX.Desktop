// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using AutoMapper;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Steam.Core;

namespace SN.withSIX.Mini.Applications
{
    public class Initializer : IInitializer, IAMInitializer
    {
        public void ConfigureAutoMapper(IProfileExpression cfg) => AutoMapperAppConfig.Setup(cfg);

        public async Task Initialize() {
            ErrorHandlerr.RegisterHandler(new GameExceptionHandler());
            Game.SteamHelper = SteamHelper.Create(); // TODO: Move
        }

        public async Task Deinitialize() {}
    }

    public interface IAMInitializer
    {
        void ConfigureAutoMapper(IProfileExpression cfg);
    }
}