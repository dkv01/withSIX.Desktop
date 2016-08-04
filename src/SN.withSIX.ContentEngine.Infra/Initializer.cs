// <copyright company="SIX Networks GmbH" file="Initializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using SN.withSIX.ContentEngine.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.ContentEngine.Infra
{
    public class Initializer : IInitializer
    {
        private readonly IContentEngine _contentEngine;

        public Initializer(IContentEngine contentEngine) {
            _contentEngine = contentEngine;
        }

        public Task Initialize() => TaskExt.Default;

        public Task Deinitialize() {
            (_contentEngine as ContentEngine)?.Dispose();
            return TaskExt.Default;
        }
    }
}