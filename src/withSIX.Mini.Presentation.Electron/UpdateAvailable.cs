// <copyright company="SIX Networks GmbH" file="UpdateAvailable.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Features.Main;

namespace withSIX.Mini.Presentation.Electron
{
    public class UpdateAvailable : IUsecaseExecutor
    {
        public async Task<object> Invoke(dynamic input) {
            var state = (UpdateState) (int) input.state;
            var version = input.version as string;
            await
                this.Send(new Applications.Features.Main.UpdateAvailable(state, version))
                    .ConfigureAwait(false);
            return true;
        }
    }
}