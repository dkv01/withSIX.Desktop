// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using AutoMapper;
using RpfGeneratorTool;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications;

namespace SN.withSIX.Mini.Plugin.GTA
{
    public class Initializer : IInitializer, IAMInitializer
    {
        public void ConfigureAutoMapper(IProfileExpression cfg) => AutoMapperPluginGTAConfig.Setup(cfg);

        public Task Initialize() {
            // TODO: Register auto through container??
            ErrorHandlerr.RegisterHandler(new GTAExceptionHandler());
            var p = new Package();

            return TaskExt.Default;
        }

        public async Task Deinitialize() {}
    }
}