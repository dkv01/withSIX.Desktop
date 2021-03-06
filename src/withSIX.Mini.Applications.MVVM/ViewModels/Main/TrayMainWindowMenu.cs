﻿// <copyright company="SIX Networks GmbH" file="TrayMainWindowMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using withSIX.Core.Applications.MVVM.Attributes;
using withSIX.Core.Applications.MVVM.Extensions;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Features;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.MVVM.ViewModels.Main
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
        public Task Login() => this.Send(new OpenWebLink(ViewType.Profile));

        [MenuItem]
        public Task Logout() => this.Send(new OpenWebLink(ViewType.Profile));

        [MenuItem]
        public Task Settings() => this.OpenWebLink(ViewType.Settings); //OpenScreenCached(new GetSettings());

        [MenuItem(IsSeparator = true)]
        public void Separator1() {}

        [MenuItem]
        public Task Community() => this.Send(new OpenWebLink(ViewType.Community));

        [MenuItem]
        public Task ReportIssues() => this.Send(new OpenWebLink(ViewType.Issues));

        [MenuItem]
        public Task ShareYourSuggestions() => this.Send(new OpenWebLink(ViewType.Suggestions));

        [MenuItem]
        public Task Help() => this.Send(new OpenWebLink(ViewType.Help));

        /*
        [MenuItem]
        public Task About() {
            return OpenAsyncQuery(new GetAbout());
        }
        */

        [MenuItem(IsSeparator = true)]
        public void Separator2() {}

        [MenuItem]
        public Task Exit() => this.Send(new Shutdown());
    }
}