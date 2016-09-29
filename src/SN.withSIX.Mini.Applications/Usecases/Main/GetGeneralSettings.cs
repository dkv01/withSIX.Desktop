// <copyright company="SIX Networks GmbH" file="GetGeneralSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Usecases.Main
{
    public class GetGeneralSettings : IAsyncQuery<GeneralSettings> {}

    public class GetGeneralSettingsHandler : DbQueryBase, IAsyncRequestHandler<GetGeneralSettings, GeneralSettings>
    {
        public GetGeneralSettingsHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task<GeneralSettings> Handle(GetGeneralSettings request)
            => (await SettingsContext.GetSettings().ConfigureAwait(false)).MapTo<GeneralSettings>();
    }

    public class GeneralSettings
    {
        public bool LaunchWithWindows { get; set; }
        public bool OptOutErrorReports { get; set; }
        public bool EnableDesktopNotifications { get; set; }
        public bool UseSystemBrowser { get; set; }
        public string Version { get; set; }
        public bool EnableDiagnosticsMode { get; set; }
        public int? ApiPort { get; set; }
    }
}