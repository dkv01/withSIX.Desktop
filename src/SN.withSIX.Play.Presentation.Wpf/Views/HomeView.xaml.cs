// <copyright company="SIX Networks GmbH" file="HomeView.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Caliburn.Micro;
using CefSharp;
using CefSharp.Wpf;
using ReactiveUI;

using withSIX.Api.Models.Collections;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.MVVM;
using SN.withSIX.Core.Logging;
using SN.withSIX.Play.Applications.ViewModels;
using SN.withSIX.Play.Applications.ViewModels.Connect;
using SN.withSIX.Play.Applications.Views;
using SN.withSIX.Play.Core.Connect;
using SN.withSIX.Play.Core.Connect.Events;
using SN.withSIX.Play.Core.Connect.Infrastructure;
using SN.withSIX.Play.Infra.Api;

namespace SN.withSIX.Play.Presentation.Wpf.Views
{
    
    public partial class HomeView : UserControl, IEnableLogging, IHandle<DoLogout>, IHandle<DoLogin>, IHomeView, IHandle<RequestOpenBrowser>
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof (HomeViewModel), typeof (HomeView),
                new PropertyMetadata(null));
        readonly ILoginHandler _loginHandler;

        public HomeView(BrowserInterop browserInterop, ILoginHandler loginHandler, IConnectApiHandler connMan) {
            InitializeComponent();
            _loginHandler = loginHandler;

            WebControl.LifeSpanHandler = new LifeSpanHandler();

            WebControl.RegisterJsObject("six_client", new Handler(browserInterop, connMan, HandleLogin));

            this.WhenActivated(d => {
                d(this.WhenAnyValue(x => x.ViewModel).BindTo(this, v => v.DataContext));
                d(this.WhenAnyValue(x => x.ViewModel)
                    .Skip(1)
                    .Subscribe(vm => {
                        d(WebControl.WhenAnyValue(x => x.IsLoading).Subscribe(x => {
                            ViewModel.ProgressState.Active = x;
                            ViewModel.CanGoBack = WebControl.CanGoBack;
                            ViewModel.CanGoForward = WebControl.CanGoForward;
                        }));
                        d(vm.Navigate.Subscribe(x => {
                            switch (x) {
                            case HomeViewModel.NavigateMode.GoBack: {
                                WebControl.BackCommand.Execute(null);
                                break;
                            }
                            case HomeViewModel.NavigateMode.Abort: {
                                WebControl.Stop();
                                break;
                            }
                            case HomeViewModel.NavigateMode.GoForward: {
                                WebControl.ForwardCommand.Execute(null);
                                break;
                            }
                            case HomeViewModel.NavigateMode.Reload: {
                                WebControl.Reload(false);
                                break;
                            }
                            }
                        }));
                    }));

                d(this.WhenAnyValue(v => v.WebControl.IsLoading)
                    .Skip(1)
                    .Subscribe(x => ViewModel.IsNavigating = x));
            });

            CommandBindings.Add(new CommandBinding(BrowserView.CopyToClipboard, OnCopyToClipboard, CanCopyToClipboard));
            CommandBindings.Add(new CommandBinding(BrowserView.OpenInSystemBrowser, OnOpenInSystemBrowser,
                CanOpenInSystemBrowser));
        }

        public void Handle(DoLogout message) => Logout();

        public void Handle(DoLogin message) => Login();

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as HomeViewModel; }
        }
        public HomeViewModel ViewModel
        {
            get { return (HomeViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        void CanOpenInSystemBrowser(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        void OnOpenInSystemBrowser(object sender, ExecutedRoutedEventArgs e) {
            Tools.Generic.TryOpenUrl(WebControl.Address);
        }

        void OnCopyToClipboard(object sender, ExecutedRoutedEventArgs e) {
            Clipboard.SetText(WebControl.Address);
        }

        void CanCopyToClipboard(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        void HandleLogin(AccessInfo info) {
            _loginHandler.HandleLogin(info).Wait();
        }

        class Handler
        {
            readonly BrowserInterop _interop;
            readonly IConnectApiHandler _connMan;
            private readonly Action<AccessInfo> _handleLogin;

            public Handler(BrowserInterop interop, IConnectApiHandler connMan, Action<AccessInfo> handleLogin) {
                _interop = interop;
                _connMan = connMan;
                _handleLogin = handleLogin;
            }

            public void subscribedToCollection(string id)
            {
                try
                {
                    _connMan.MessageBus.SendMessage(new SubscribedToCollection(Guid.Parse(id)));
                }
                catch (Exception ex)
                {
                    MainLog.Logger.FormattedWarnException(ex, "error during JS exec");
                }
            }

            public void unsubscribedFromCollection(string id)
            {
                try
                {
                    _connMan.MessageBus.SendMessage(new UnsubscribedFromCollection(Guid.Parse(id)));
                }
                catch (Exception ex)
                {
                    MainLog.Logger.FormattedWarnException(ex, "error during JS exec");
                }
            }

            public void open_pws_uri(string argument) {
                try {
                    _interop.OpenPwsUri(argument);
                } catch (Exception ex) {
                    MainLog.Logger.FormattedWarnException(ex, "error during JS exec");
                }
            }

            public void login(string accessToken) {
                try {
                    _handleLogin(new AccessInfo() { AccessToken = accessToken });
                } catch (Exception ex) {
                    MainLog.Logger.FormattedWarnException(ex, "error during JS exec");
                }
            }

            public void refresh_login() {
                try {
                    _interop.RefreshLogin();
                } catch (Exception ex) {
                    MainLog.Logger.FormattedWarnException(ex, "error during JS exec");
                }
            }
        }

        #region IHandle events

/*
        public void Handle(ApiKeyUpdated message) {
            if (!string.IsNullOrWhiteSpace(message.ApiKey))
                Reload();
        }
*/

        void Reload() => UiHelper.TryOnUiThread(() => WebControl.Reload(false));

        void Login() {
            UiHelper.TryOnUiThread(() => {
                try {
                    WebControl.ExecuteScriptAsync("window.w6Cheat.api.login();");
                } catch (InvalidOperationException ex) {
                    MainLog.Logger.FormattedWarnException(ex, "Could not login the webpage");
                }
            });
        }

        void Logout() {
            UiHelper.TryOnUiThread(() => {
                try {
                    WebControl.ExecuteScriptAsync("window.w6Cheat.api.logout();");
                } catch (InvalidOperationException ex) {
                    MainLog.Logger.FormattedWarnException(ex, "Could not logout of the webpage");
                }
            });
        }

        #endregion

        public void Handle(RequestOpenBrowser message) {
            UiHelper.TryOnUiThread(() => {
                var currentAddress = WebControl.Address == null ? null : new Uri(WebControl.Address);
                var url = message.Url.ToString();
                var relativeUrl = currentAddress != null && message.Url.Host == currentAddress.Host
                    ? message.Url.PathAndQuery
                    : message.Url.ToString();
                if (currentAddress != null) {
                    WebControl.EvaluateScriptAsync(
                        $"if (window.w6Cheat && window.w6Cheat.api.navigate) {"{ window.w6Cheat.api.navigate('" + relativeUrl + "'); }"} else {"{ window.location.href = '" + url + "'; }"}");
                } else {
                    WebControl.SetValue(ChromiumWebBrowser.AddressProperty, url);
                }
            });
        }
    }

    public class LifeSpanHandler : ILifeSpanHandler
    {
        public bool OnBeforePopup(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName,
            WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo,
            IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser) {
            windowInfo.X = 640;
            windowInfo.Y = 640;

            newBrowser = null;
            return false;
        }

        public void OnAfterCreated(IWebBrowser browserControl, IBrowser browser) {}

        public bool DoClose(IWebBrowser browserControl, IBrowser browser) => false;

        public void OnBeforeClose(IWebBrowser browserControl, IBrowser browser) {}
    }
}