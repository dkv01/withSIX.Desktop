// <copyright company="SIX Networks GmbH" file="WpfDialogManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using ReactiveUI;
using SN.withSIX.Core.Applications.MVVM.ViewModels.Dialogs;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using Action = System.Action;
using ViewLocator = ReactiveUI.ViewLocator;

namespace SN.withSIX.Core.Presentation.Wpf.Services
{
    public class WpfCustomDialogManager : ISpecialDialogManager, IEnableLogging, IPresentationService
    {
        private readonly IWindowManager _windowManager;

        public WpfCustomDialogManager(IWindowManager windowManager) {
            _windowManager = windowManager;
            if (Application.Current != null)
                Common.App.IsWpfApp = true;
        }

        public async Task<SixMessageBoxResult> MessageBox(MessageBoxDialogParams dialogParams) {
            var ev = GetMessageBoxViewModel(dialogParams);
            await ShowDialog(ev);
            var vm = ev;
            return vm.Result;
        }

        public async Task<Tuple<string, string, bool?>> UserNamePasswordDialog(string title, string location) {
            var ev = new EnterUserNamePasswordViewModel {
                DisplayName = title,
                Location = location
            };
            var result = await ShowDialog(ev).ConfigureAwait(false);
            return Tuple.Create(ev.Username, ev.Password, result);
        }

        public async Task<Tuple<SixMessageBoxResult, string>> ShowEnterConfirmDialog(string msg, string defaultInput) {
            var vm = new EnterConfirmViewModel {
                Message = msg,
                Input = defaultInput,
                RememberedState = false
            };

            //await ShowMetroDialog(vm);
            await ShowDialog(vm).ConfigureAwait(false);

            return
                new Tuple<SixMessageBoxResult, string>(
                    vm.Canceled
                        ? SixMessageBoxResult.Cancel
                        : (vm.RememberedState == true ? SixMessageBoxResult.YesRemember : SixMessageBoxResult.Yes),
                    vm.Input);
        }


        public Task ShowWindow(object vm, IDictionary<string, object> overrideSettings = null) {
            var defaultSettings = new Dictionary<string, object> {
                {"ShowInTaskbar", false},
                {"WindowStyle", WindowStyle.None}
            };
            return
                Schedule(() => _windowManager.ShowWindow(vm, null, defaultSettings.MergeIfOverrides(overrideSettings)));
        }

        public Task ShowPopup(object vm, IDictionary<string, object> overrideSettings = null) {
            var defaultSettings = new Dictionary<string, object> {
                {"ShowInTaskbar", false},
                {"WindowStyle", WindowStyle.None},
                {"StaysOpen", false}
            };

            return Schedule(() => _windowManager.ShowPopup(vm, null, defaultSettings.MergeIfOverrides(overrideSettings)));
        }


        public Task<bool?> ShowDialog(object ev, IDictionary<string, object> overrideSettings = null) {
            var defaultSettings = GetDefaultDialogSettings();
            return
                Schedule(() => _windowManager.ShowDialog(ev, null, defaultSettings.MergeIfOverrides(overrideSettings)));
        }

        public static Task<T> Schedule<T>(Func<T> t) => Application.Current.Dispatcher.InvokeAsync(t).Task;

        public static Task Schedule(Action t) => Application.Current.Dispatcher.InvokeAsync(t).Task;

        // TODO: Consider using messageboxes directly like any other View with actions?
        protected async Task<SixMessageBoxResult> MetroMessageBox(MessageBoxDialogParams dialogParams) {
            ConfirmAccess();
            var ev = GetMetroMessageBoxViewModel(dialogParams);
            await ShowMetroDialog(ev);
            var vm = ev;
            return vm.Result;
        }

        /// <summary>
        ///     DO NOT CALL .RESULT or .WAIT from UI thread!
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        protected async Task<bool?> ShowMetroDialog(IMetroDialog model) {
            // Da faq mahapps!!!
            var resolvedView = ViewLocator.Current.ResolveView(model);
            resolvedView.ViewModel = model;

            var view = resolvedView as BaseMetroDialog;
            if (view != null)
                return await HandleMetroDialog(model, view);

            var dialog = new CustomDialog {Content = resolvedView};
            using (model.WhenAnyValue(x => x.DisplayName).BindTo(dialog, x => x.Title))
                return await HandleMetroDialog(model, dialog);
        }

        static MessageBoxViewModel GetMessageBoxViewModel(MessageBoxDialogParams dialogParams) {
            MessageBoxViewModel ev;
            if (dialogParams.IgnoreContent) {
                ev = new MessageBoxViewModel(dialogParams.Message, dialogParams.Title,
                    GetButton(dialogParams.Buttons), dialogParams.RememberedState);
            } else {
                ev = new MessageBoxViewModel(dialogParams.Message, dialogParams.Title,
                    GetButton(dialogParams.Buttons), dialogParams.RememberedState) {
                        GreenButtonContent = dialogParams.GreenContent,
                        BlueButtonContent = dialogParams.BlueContent,
                        RedButtonContent = dialogParams.RedContent
                    };
            }
            return ev;
        }

        static MetroMessageBoxViewModel GetMetroMessageBoxViewModel(MessageBoxDialogParams dialogParams) {
            MetroMessageBoxViewModel ev;
            if (dialogParams.IgnoreContent) {
                ev = new MetroMessageBoxViewModel(dialogParams.Message, dialogParams.Title,
                    GetButton(dialogParams.Buttons), dialogParams.RememberedState);
            } else {
                ev = new MetroMessageBoxViewModel(dialogParams.Message, dialogParams.Title,
                    GetButton(dialogParams.Buttons), dialogParams.RememberedState) {
                        GreenButtonContent = dialogParams.GreenContent,
                        BlueButtonContent = dialogParams.BlueContent,
                        RedButtonContent = dialogParams.RedContent
                    };
            }
            return ev;
        }

        public static void ConfirmAccess() {
            if (!Application.Current.Dispatcher.CheckAccess())
                throw new InvalidOperationException("Must be called from UI thread");
        }

        static Dictionary<string, object> GetDefaultDialogSettings() => new Dictionary<string, object> {
            {"ShowInTaskbar", false},
            {"WindowStyle", WindowStyle.None}
        };

        static async Task<bool?> HandleMetroDialog(IMetroDialog model, BaseMetroDialog dialog) {
            var window = (MetroWindow) Application.Current.MainWindow;
            var tcs = new TaskCompletionSource<bool?>();
            model.Close = CreateCommand(dialog, window, tcs);
            await window.ShowMetroDialogAsync(dialog);
            return await tcs.Task;
        }

        static ReactiveCommand<bool?> CreateCommand(BaseMetroDialog dialog, MetroWindow window,
            TaskCompletionSource<bool?> tcs) {
            var command = ReactiveCommand.CreateAsyncTask(async x => {
                await window.HideMetroDialogAsync(dialog);
                return (bool?) x;
            });
            SetupCommand(tcs, command);
            return command;
        }

        static void SetupCommand(TaskCompletionSource<bool?> tcs, ReactiveCommand<bool?> command) {
            var d = new CompositeDisposable {command};
            d.Add(command.ThrownExceptions.Subscribe(x => {
                tcs.SetException(x);
                d.Dispose();
            }));
            d.Add(command.Subscribe(x => {
                tcs.SetResult(x);
                d.Dispose();
            }));
        }

        static MessageBoxButton GetButton(SixMessageBoxButton button)
            => (MessageBoxButton) Enum.Parse(typeof (MessageBoxButton), button.ToString());
    }

    /// <summary>
    ///     Re-usable dialogs
    /// </summary>
    // TODO: It would probably be nicer if we would not have to manually marshall these calls to the UI thread,
    // but instead (like RXUI), rely on the caller to be on the right thread already?
    // Since ViewModels are supposed to open these it could make sense.
    // The main concern with current approach is what to do when already on the UI thread... as .Wait/.Result can cause a deadlock...
    public class WpfDialogManager : IDialogManager, IEnableLogging
    {
        private readonly ISpecialDialogManager _wm;

        public WpfDialogManager(ISpecialDialogManager wm) {
            _wm = wm;
            if (Application.Current != null)
                Common.App.IsWpfApp = true;
        }

        public Task<string> BrowseForFolder(string selectedPath = null,
            string title = null) => WpfCustomDialogManager.Schedule(() => BrowseForFolderInternal(selectedPath, title));

        public Task<string> BrowseForFile(string initialDirectory = null,
            string title = null,
            string defaultExt = null, bool checkFileExists = true)
            =>
                WpfCustomDialogManager.Schedule(
                    () => BrowseForFileInternal(initialDirectory, title, defaultExt, checkFileExists));

        public Task<SixMessageBoxResult> MessageBox(MessageBoxDialogParams dialogParams) => _wm.MessageBox(dialogParams);

        public async Task<bool> ExceptionDialog(Exception e, string message,
            string title = null, object owner = null) {
            this.Logger().FormattedWarnException(e);
            if (Common.Flags.IgnoreErrorDialogs)
                return false;

            if (title == null)
                title = "A problem has occurred";

            await
                MessageBox(new MessageBoxDialogParams(title, message + "\n" + e.Format(), SixMessageBoxButton.OK))
                    .ConfigureAwait(false);
            return true;
        }
        protected string BrowseForFolderInternal(string selectedPath = null, string title = null) {
            WpfCustomDialogManager.ConfirmAccess();
            var dialog = new VistaFolderBrowserDialog {SelectedPath = selectedPath, Description = title};
            return dialog.ShowDialog() == true ? dialog.SelectedPath : null;
        }

        protected string BrowseForFileInternal(string initialDirectory = null, string title = null,
            string defaultExt = null,
            bool checkFileExists = true) {
            WpfCustomDialogManager.ConfirmAccess();
            var dialog = new OpenFileDialog {
                InitialDirectory = initialDirectory,
                DefaultExt = defaultExt,
                CheckFileExists = checkFileExists,
                Title = title
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }

    class ExceptionDialogThrownException : Exception
    {
        public ExceptionDialogThrownException(string message, Exception exception) : base(message, exception) {}
    }
}