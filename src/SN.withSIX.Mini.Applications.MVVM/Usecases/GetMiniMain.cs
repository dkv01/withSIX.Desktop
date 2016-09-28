// <copyright company="SIX Networks GmbH" file="GetMiniMain.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.MVVM.ViewModels;
using SN.withSIX.Mini.Applications.MVVM.ViewModels.Main;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Applications.Usecases;

namespace SN.withSIX.Mini.Applications.MVVM.Usecases
{
    public class GetMiniMain : IAsyncQuery<IMiniMainWindowViewModel> {}

    public class GetMiniMainHandler : DbQueryBase, IAsyncRequestHandler<GetMiniMain, IMiniMainWindowViewModel>
    {
        readonly IStateHandler _stateHandler;

        public GetMiniMainHandler(IDbContextLocator dbContextLocator, IStateHandler stateHandler)
            : base(dbContextLocator) {
            _stateHandler = stateHandler;
        }

        public async Task<IMiniMainWindowViewModel> Handle(GetMiniMain request)
            =>
            new MiniMainWindowViewModel(new StatusViewModel(_stateHandler.StatusObservable),
                new TrayMainWindowMenu((await SettingsContext.GetSettings().ConfigureAwait(false)).Secure.Login ??
                                       LoginInfo.Default));
    }
}