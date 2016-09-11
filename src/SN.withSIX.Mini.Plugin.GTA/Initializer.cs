// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using RpfGeneratorTool;
using SN.withSIX.Core.Applications.Services;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Plugin.GTA
{
    public class Initializer : IInitializer
    {
        public Task Initialize() {
            var p = new Package();

            return TaskExt.Default;
        }

        public async Task Deinitialize() {}
    }
}