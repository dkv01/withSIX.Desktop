// <copyright company="SIX Networks GmbH" file="MiniMainWindowViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using ReactiveUI;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.MVVM.Extensions;
using SN.withSIX.Core.Applications.MVVM.Services;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.MVVM.ViewModels.Main;
using SN.withSIX.Mini.Applications.Services;

namespace SN.withSIX.Mini.Applications.MVVM.ViewModels
{
    public class MiniMainWindowViewModel : ScreenViewModel, IMiniMainWindowViewModel
    {
        readonly ObservableAsPropertyHelper<string> _taskbarToolTip;

        IStatusViewModel _status;

        public MiniMainWindowViewModel(IStatusViewModel status, TrayMainWindowMenu menu) {
            Menu = menu;
            _status = status;
            _taskbarToolTip = this.WhenAnyValue(x => x.DisplayName, x => x.Status.Status,
                FormatTaskbarToolTip)
                .ToProperty(this, x => x.TaskbarToolTip);
            OpenPopup = ReactiveCommand.Create();
            ShowNotification = ReactiveCommand.CreateAsyncTask(async x => (ITrayNotificationViewModel) x);
            Deactivate = ReactiveCommand.Create().DefaultSetup("Deactivate");
            Deactivate.Subscribe(x => Close.Execute(null));

            // TODO: Make this a setting?
            /*            Listen<ApiUserActionStarted>()
                .ObserveOnMainThread()
                .InvokeCommand(OpenPopup);*/
            RxExtensions.ObserveOnMainThread<ClientInfoUpdated>(this.Listen<ClientInfoUpdated>()
                    .Where(x => x.Info.UpdateState == AppUpdateState.Updating))
                .InvokeCommand(OpenPopup);
            RxExtensions.ObserveOnMainThread<TrayNotificationViewModel>(this.Listen<ShowTrayNotification>()
                    .Select(x => new TrayNotificationViewModel(x.Subject, x.Text, x.CloseIn, x.Actions)))
                .InvokeCommand(ShowNotification);
        }

        public TrayMainWindowMenu Menu { get; }

        public IStatusViewModel Status
        {
            get { return _status; }
            private set { this.RaiseAndSetIfChanged(ref _status, value); }
        }

        public IReactiveCommand<object> Deactivate { get; }
        public override string DisplayName => Consts.WindowTitle;
        public string TaskbarToolTip => _taskbarToolTip.Value;
        public ReactiveCommand<object> OpenPopup { get; }
        public IReactiveCommand<ITrayNotificationViewModel> ShowNotification { get; }

        public string FormatTaskbarToolTip(string s, ActionTabState statusModel) {
            var baseText = s + " v" + Consts.ProductVersion;
            return statusModel == null
                ? baseText
                : baseText + "\n" + (statusModel.ChildAction.Details ?? statusModel.Text);
        }
    }

    public class ShowTrayNotification : IDomainEvent
    {
        public ShowTrayNotification(string subject, string text, string icon = null, TimeSpan? expirationTime = null,
            params TrayAction[] actions) {
            Subject = subject;
            CloseIn = expirationTime;
            Text = text;
            Icon = icon;
            Actions = actions;
        }

        public string Subject { get; }
        public string Text { get; }
        public string Icon { get; }
        public TimeSpan? CloseIn { get; }
        public ICollection<TrayAction> Actions { get; }
    }

    public interface IMiniMainWindowViewModel : IScreenViewModel
    {
        IStatusViewModel Status { get; }
        string TaskbarToolTip { get; }
        ReactiveCommand<object> OpenPopup { get; }
        IReactiveCommand<ITrayNotificationViewModel> ShowNotification { get; }
        IReactiveCommand<object> Deactivate { get; }
        TrayMainWindowMenu Menu { get; }
    }
}