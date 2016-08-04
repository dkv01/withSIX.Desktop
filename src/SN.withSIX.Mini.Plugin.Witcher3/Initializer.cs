// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using AutoMapper;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Plugin.Witcher3
{
    public class Initializer : IInitializer, IAMInitializer
    {
        public void ConfigureAutoMapper(IProfileExpression cfg) => AutoMapperPluginWitcher3Config.Setup(cfg);

        public Task Initialize() {
            // TODO: Register auto through container??

            return TaskExt.Default;
        }


        public async Task Deinitialize() {}
    }
}