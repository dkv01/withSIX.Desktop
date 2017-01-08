// <copyright company="SIX Networks GmbH" file="SaveGeneralSettings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features.Main
{
    public class SaveGeneralSettings : IVoidCommand
    {
        public GeneralSettings Settings { get; set; }
    }

    public class SaveGeneralSettingsHandler : DbCommandBase, IAsyncRequestHandler<SaveGeneralSettings>
    {
        public SaveGeneralSettingsHandler(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}

        public async Task Handle(SaveGeneralSettings request) {
            request.Settings.MapTo(await SettingsContext.GetSettings().ConfigureAwait(false));

            
        }
    }
}