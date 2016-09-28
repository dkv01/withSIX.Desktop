// <copyright company="SIX Networks GmbH" file="TrayMainWindowMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using SN.withSIX.Core.Applications.MVVM.Attributes;
using SN.withSIX.Core.Applications.MVVM.Extensions;
using SN.withSIX.Core.Applications.MVVM.ViewModels;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Applications.Usecases;

namespace SN.withSIX.Mini.Applications.MVVM.ViewModels.Main
{
    public class TrayMainWindowMenu : CMBase, IUsecaseExecutor
    {
        readonly IMenuItem _login;
        readonly IMenuItem _loginInfo;
        readonly IMenuItem _logout;

        public TrayMainWindowMenu(LoginInfo loginInfo) {
            _loginInfo = new MenuItem("logged in as ...") {IsEnabled = false, IsVisible = false};
            _login = Items.First(x => x.AsyncAction == Login);
            _logout = Items.First(x => x.AsyncAction == Logout);
            Items.Insert(0, _loginInfo);
            UpdateLogin(loginInfo);
            // TODO: Activation doesnt work atm :S
            // this.WhenActivated(d => d(ListenIncludeLatest<LoginChanged>().Select(x => x == null ? null : x.LoginInfo).ObserveOnMainThread().Subscribe(UpdateLogin)));
            this.Listen<LoginChanged>()
                .Select(x => x.LoginInfo).ObserveOnMainThread()
                .Subscribe(UpdateLogin);
        }

        void UpdateLogin(LoginInfo loginInfo) {
            // TODO: Monitor realtime..
            // TODO: Don't reach into Cheat..
            _loginInfo.IsVisible = loginInfo.IsLoggedIn;
            _login.IsVisible = !loginInfo.IsLoggedIn;
            _logout.IsVisible = !_login.IsVisible;
            if (loginInfo.IsLoggedIn)
                _loginInfo.Name = "logged in as " + loginInfo.Account.UserName;
        }

        [MenuItem]
        public Task Login() => this.SendAsync(new OpenWebLink(ViewType.Profile));

        [MenuItem]
        public Task Logout() => this.SendAsync(new OpenWebLink(ViewType.Profile));

        [MenuItem]
        public Task Settings() => this.OpenWebLink(ViewType.Settings); //OpenScreenCached(new GetSettings());

        [MenuItem(IsSeparator = true)]
        public void Separator1() {}

        [MenuItem]
        public Task Community() => this.SendAsync(new OpenWebLink(ViewType.Community));

        [MenuItem]
        public Task ReportIssues() => this.SendAsync(new OpenWebLink(ViewType.Issues));

        [MenuItem]
        public Task ShareYourSuggestions() => this.SendAsync(new OpenWebLink(ViewType.Suggestions));

        [MenuItem]
        public Task Help() => this.SendAsync(new OpenWebLink(ViewType.Help));

        /*
        [MenuItem]
        public Task About() {
            return OpenAsyncQuery(new GetAbout());
        }
        */

        [MenuItem(IsSeparator = true)]
        public void Separator2() {}

        [MenuItem]
        public Task Exit() => this.SendAsync(new Shutdown());
    }
}