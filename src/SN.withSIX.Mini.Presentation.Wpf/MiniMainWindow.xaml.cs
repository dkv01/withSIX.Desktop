// <copyright company="SIX Networks GmbH" file="MiniMainWindow.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using MahApps.Metro.Controls;
using ReactiveUI;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.MVVM.ViewModels;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Presentation.Wpf.Extensions;
using SN.withSIX.Mini.Presentation.Wpf.Services;
using SN.withSIX.Mini.Presentation.Wpf.Views;

namespace SN.withSIX.Mini.Presentation.Wpf
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MiniMainWindow : MetroWindow, IViewFor<IMiniMainWindowViewModel>, IUsecaseExecutor
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof (IMiniMainWindowViewModel), typeof (MiniMainWindow),
                new PropertyMetadata(null));

        public MiniMainWindow() {
            InitializeComponent();
            Closing += OnClosing;

            var img = new BitmapImage(new Uri("pack://application:,,,/Sync;component/app.ico"));
            var busyimg = new BitmapImage(new Uri("pack://application:,,,/Sync;component/app-busy.ico"));
            var notBusyImage = Consts.ReleaseTitle != null ? busyimg : img;


            this.WhenActivated(d => {
                this.SetupScreen<IMiniMainWindowViewModel>(d, true);
                d(ViewModel.OpenPopup
                    .ObserveOn(ThreadPoolScheduler.Instance)
                    .ConcatTask(ShowAndActivate)
                    .Subscribe());
                d(this.OneWayBind(ViewModel, vm => vm.TaskbarToolTip, v => v.tbInfo.Description));
                d(this.OneWayBind(ViewModel, vm => vm.Status.Status.Progress, v => v.tbInfo.ProgressValue,
                    d1 => d1/100.0));
                d(this.OneWayBind(ViewModel, vm => vm.Status.Status, v => v.tbInfo.ProgressState,
                    ToProgressState));
                d(ViewModel.WhenAnyObservable(x => x.ShowNotification).Subscribe(Notify));
                d(this.OneWayBind(ViewModel, vm => vm.DisplayName, v => v.Title));
                //d(this.Bind(ViewModel, vm => vm.TrayViewModel, v => v.TrayMainWindow.ViewModel));
                d(this.OneWayBind(ViewModel, vm => vm.TaskbarToolTip, v => v.TaskbarIcon.ToolTipText));
                d(this.OneWayBind(ViewModel, vm => vm.Status.Status.Type, v => v.TaskbarIcon.IconSource,
                    b => b == ActionType.Start ? busyimg : notBusyImage));
                d(Cheat.MessageBus.Listen<ScreenOpened>().InvokeCommand(ViewModel, vm => vm.Deactivate));
                d(this.Events().Deactivated.InvokeCommand(ViewModel, vm => vm.Deactivate));
            });
            TaskbarIcon.TrayLeftMouseUp += (sender, args) => ViewModel.OpenPopup.Execute(null);
            //TaskbarIcon.TrayRightMouseUp += (sender, args) => ViewModel.OpenPopup.Execute(null);

            TaskbarIcon.TrayMiddleMouseUp += (sender, args) => ViewModel.OpenPopup.Execute(null);
            //TaskbarIcon.PopupActivation = PopupActivationMode.All;
        }

        public IMiniMainWindowViewModel ViewModel
        {
            get { return (IMiniMainWindowViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (IMiniMainWindowViewModel) value; }
        }

        Task ShowAndActivate() {
            //Show();
            //Activate();
            return this.SendAsync(new OpenWebLink(ViewType.ClientLanding));
        }

        static TaskbarItemProgressState ToProgressState(ActionTabState b) {
            if (b == null)
                return TaskbarItemProgressState.None;
            switch (b.Type) {
            case ActionType.Fail:
                return TaskbarItemProgressState.Error;
            default:
                return b.Progress == null ? TaskbarItemProgressState.Indeterminate : TaskbarItemProgressState.Normal;
            }
        }

        void Notify(ITrayNotificationViewModel notification) {
            Dispatcher.InvokeAsync(
                () => TaskbarIcon.ShowCustomBalloon(
                    new TrayNotification {
                        ViewModel = notification
                    },
                    PopupAnimation.Fade,
                    notification.CloseIn == null ? null : (int?) notification.CloseIn.Value.TotalMilliseconds));
            // TODO: configurable delay etc
        }

        void OnClosing(object sender, CancelEventArgs e) {
            if (Cheat.IsShuttingDown) {
                ViewModel.IsOpen = false;
                return;
            }
            e.Cancel = true;
            Hide();
        }
    }
}