// <copyright company="SIX Networks GmbH" file="StatusViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Windows.Input;
using ReactiveUI;
using withSIX.Core.Applications.MVVM.Extensions;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Usecases.Main;

namespace withSIX.Mini.Applications.MVVM.ViewModels.Main
{
    // TODO: Decide if Icons, Text or Colors are supposed to be part of the VM, or rather part of the Presentation layer..
    public class StatusViewModel : ViewModel, IStatusViewModel
    {
        ActionTabState _status;

        public StatusViewModel(IObservable<ActionTabState> statusObservable) {
            Abort = ReactiveCommand.CreateAsyncTask(
                    async x => await this.SendAsync(new CancelAll()).ConfigureAwait(false))
                .DefaultSetup("Cancel");

            // We won't get activated because we no longer have a window ;-)
            statusObservable.ObserveOnMainThread().BindTo(this, x => x.Status);
        }

        public ActionTabState Status
        {
            get { return _status; }
            set { this.RaiseAndSetIfChanged(ref _status, value); }
        }
        public ICommand Abort { get; }
    }

    public interface IStatusViewModel
    {
        ActionTabState Status { get; }
        ICommand Abort { get; }
    }
}