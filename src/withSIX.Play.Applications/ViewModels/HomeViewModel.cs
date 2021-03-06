// <copyright company="SIX Networks GmbH" file="HomeViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using Caliburn.Micro;
using ReactiveUI;

using withSIX.Core;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.MVVM.Extensions;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.Services;
using withSIX.Play.Applications.ViewModels.Connect;
using withSIX.Play.Core;
using withSIX.Play.Core.Connect.Events;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy.Events;
using ReactiveCommand = ReactiveUI.Legacy.ReactiveCommand;

namespace withSIX.Play.Applications.ViewModels
{
    
    public class HomeViewModel : ModuleViewModelBase, IHandle<ActiveGameChanged>, IHandle<RequestOpenBrowser>, IHandle<DoLogout>, IHandle<DoLogin>
    {
        public enum NavigateMode
        {
            Reload,
            GoBack,
            GoForward,
            Abort
        }

        bool _canGoBack;
        bool _canGoForward;
        bool _isNavigating;
        Uri _url;

        public HomeViewModel() {
            ModuleName = ControllerModules.Home;
            DisplayName = null;
            GoHome();
            Navigate = new Subject<NavigateMode>();

            DisplayName = "Home";

            ReactiveUI.ReactiveCommand.Create(this.WhenAnyValue(x => x.IsNavigating).Select(x => !x))
                .SetNewCommand(this, x => x.ReloadCommand)
                .Subscribe(x => Reload());
            ReactiveUI.ReactiveCommand.Create(this.WhenAnyValue(x => x.IsNavigating))
                .SetNewCommand(this, x => x.AbortCommand)
                .Subscribe(x => Abort());

            this.SetCommand(x => x.OpenInExternalCommand).Subscribe(() => Tools.Generic.TryOpenUrl(Url));
            this.SetCommand(x => x.CopyUrlCommand).Subscribe(() => Clipboard.SetText(Url.ToString()));
        }

        public ReactiveCommand<object> AbortCommand { get; private set; }
        public ReactiveCommand<object> ReloadCommand { get; private set; }
        public ReactiveCommand OpenInExternalCommand { get; private set; }
        public ReactiveCommand CopyUrlCommand { get; private set; }
        public Subject<NavigateMode> Navigate { get; }
        public Uri Url
        {
            get { return _url; }
            set
            {
                SetProperty(ref _url, value);
                OnPropertyChanged(); // wtf double?
            }
        }
        public bool CanGoBack
        {
            get { return _canGoBack; }
            set { SetProperty(ref _canGoBack, value); }
        }
        public bool CanGoForward
        {
            get { return _canGoForward; }
            set { SetProperty(ref _canGoForward, value); }
        }
        public bool IsNavigating
        {
            get { return _isNavigating; }
            set { SetProperty(ref _isNavigating, value); }
        }

        void Abort() {
            Navigate.OnNext(NavigateMode.Abort);
        }

        public void GoHome() {
            Url = GetGameStreamUrl();
        }

        // TODO: save previous premium state so we can start at https
        static Uri GetUrl(Game game) => string.IsNullOrWhiteSpace(game.MetaData.Slug)
    ? CommonUrls.BlogUrl
    : Tools.Transfer.JoinUri(CommonUrls.PlayUrl, game.MetaData.Slug, "stream");

        public Uri GetLatestNewsLink() => GetGameStreamUrl();

        static Uri GetGameStreamUrl() => GetUrl(DomainEvilGlobal.SelectedGame.ActiveGame);

        void Reload() {
            Navigate.OnNext(NavigateMode.Reload);
        }

        public void GoBack() {
            Navigate.OnNext(NavigateMode.GoBack);
        }

        public void GoForward() {
            Navigate.OnNext(NavigateMode.GoForward);
        }

        #region IHandle events

        public void Handle(ActiveGameChanged message) {
            Url = GetUrl(message.Game);
        }

        public void Handle(RequestOpenBrowser message) => Open();


        public void Handle(DoLogout message) => Open();

        public void Handle(DoLogin message) => Open();

        #endregion
    }
}