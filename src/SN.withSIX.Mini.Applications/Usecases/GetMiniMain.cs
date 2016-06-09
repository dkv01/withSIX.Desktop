// <copyright company="SIX Networks GmbH" file="GetMiniMain.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Applications.ViewModels;
using SN.withSIX.Mini.Applications.ViewModels.Main;
using Squirrel;

namespace SN.withSIX.Mini.Applications.Usecases
{
    public interface ISquirrelApp
    {
        Task<string> GetNewVersion();
    }

    public interface ISquirrelUpdater
    {
        Task<UpdateInfo> CheckForUpdates();
        Task<ReleaseEntry> UpdateApp(Action<int> progressAction);
    }

    public class GetMiniMain : IAsyncQuery<IMiniMainWindowViewModel> {}

    public class GetMiniMainHandler : DbQueryBase, IAsyncRequestHandler<GetMiniMain, IMiniMainWindowViewModel>
    {
        readonly IStateHandler _stateHandler;

        public GetMiniMainHandler(IDbContextLocator dbContextLocator, IStateHandler stateHandler)
            : base(dbContextLocator) {
            _stateHandler = stateHandler;
        }

        public async Task<IMiniMainWindowViewModel> HandleAsync(GetMiniMain request)
            =>
                new MiniMainWindowViewModel(new StatusViewModel(_stateHandler.StatusObservable),
                    new TrayMainWindowMenu((await SettingsContext.GetSettings().ConfigureAwait(false)).Secure.Login ??
                                           LoginInfo.Default));
    }
}