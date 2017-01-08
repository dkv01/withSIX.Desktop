// <copyright company="SIX Networks GmbH" file="StartInDiagnosticsMode.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Applications.Services;

namespace withSIX.Mini.Applications.Features.Settings
{
    public class SaveLogs : IAsyncVoidCommand {}

    public class SaveLogsHandler : IAsyncRequestHandler<SaveLogs>
    {
        readonly IRestarter _restarter;

        public SaveLogsHandler(IRestarter restarter) {
            _restarter = restarter;
        }

        public async Task Handle(SaveLogs request) {
            var path =
                Common.Paths.TempPath.GetChildFileWithName("Sync diagnostics " + DateTime.UtcNow.ToFileTimeUtc() +
                                                           ".zip");
            Common.Paths.TempPath.MakeSurePathExists();
            await ErrorHandlerr.GenerateDiagnosticZip(path).ConfigureAwait(false);
            Tools.FileUtil.SelectInExplorer(path.ToString());
            
        }
    }

    public class StartInDiagnosticsMode : IAsyncVoidCommand {}

    public class StartInDiagnosticsModeHandler : IAsyncRequestHandler<StartInDiagnosticsMode>
    {
        readonly IRestarter _restarter;

        public StartInDiagnosticsModeHandler(IRestarter restarter) {
            _restarter = restarter;
        }

        public async Task Handle(StartInDiagnosticsMode request) {
            Common.Flags.Verbose = true;
            _restarter.RestartWithoutElevation(
                Tools.UacHelper.GetStartupParameters().Concat(new[] {"--verbose"}).ToArray());
            
        }
    }
}