// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Services;

namespace withSIX.Mini.Plugin.GTA
{
    public class Initializer : IInitializer
    {
        public Task Initialize() {
            //var p = new Package();

            return TaskExt.Default;
        }

        public async Task Deinitialize() {}
    }
}