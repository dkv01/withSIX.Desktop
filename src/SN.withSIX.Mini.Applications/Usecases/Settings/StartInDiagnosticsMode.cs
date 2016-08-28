// <copyright company="SIX Networks GmbH" file="StartInDiagnosticsMode.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Applications.Usecases.Settings
{
    public class SaveLogs : IAsyncVoidCommand {}

    public class SaveLogsHandler : IAsyncVoidCommandHandler<SaveLogs>
    {
        readonly IRestarter _restarter;

        public SaveLogsHandler(IRestarter restarter) {
            _restarter = restarter;
        }

        public async Task<Unit> Handle(SaveLogs request) {
            var path =
                Common.Paths.TempPath.GetChildFileWithName("Sync diagnostics " + DateTime.UtcNow.ToFileTimeUtc() +
                                                           ".zip");
            Common.Paths.TempPath.MakeSurePathExists();
            await ErrorHandlerr.GenerateDiagnosticZip(path).ConfigureAwait(false);
            Tools.FileUtil.SelectInExplorer(path.ToString());
            return Unit.Value;
        }
    }

    public class StartInDiagnosticsMode : IAsyncVoidCommand {}

    public class StartInDiagnosticsModeHandler : IAsyncVoidCommandHandler<StartInDiagnosticsMode>
    {
        readonly IRestarter _restarter;

        public StartInDiagnosticsModeHandler(IRestarter restarter) {
            _restarter = restarter;
        }

        public async Task<Unit> Handle(StartInDiagnosticsMode request) {
            Common.Flags.Verbose = true;
            _restarter.RestartWithoutElevation(
                Tools.Generic.GetStartupParameters().Concat(new[] {"--verbose"}).ToArray());
            return Unit.Value;
        }
    }
}