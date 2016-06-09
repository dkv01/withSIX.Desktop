// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using AutoMapper;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications;

namespace SN.withSIX.Mini.Plugin.Homeworld
{
    public class Initializer : IInitializer, IAMInitializer
    {
        public void ConfigureAutoMapper(IMapperConfiguration cfg) => AutoMapperPluginHomeworldConfig.Setup(cfg);

        public async Task Initialize() {}

        public async Task Deinitialize() {}
    }
}