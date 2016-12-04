// <copyright company="SIX Networks GmbH" file="GetMiniMain.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using MediatR;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Features;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Applications.MVVM.ViewModels;
using withSIX.Mini.Applications.MVVM.ViewModels.Main;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.MVVM.Features
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