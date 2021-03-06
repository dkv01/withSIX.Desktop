﻿// <copyright company="SIX Networks GmbH" file="ScreenViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows.Input;
using ReactiveUI;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Features;
using withSIX.Mini.Applications.MVVM.Services.Components;

namespace withSIX.Mini.Applications.MVVM.ViewModels
{
    public interface IScreenViewModel : IViewModel, IScreen, IActivatableScreen, IHaveDisplayName
    {
        ICommand Help { get; }
    }

    public interface IHaveDisplayName
    {
        string DisplayName { get; }
    }

    public interface IHaveIcon
    {
        string Icon { get; }
    }

    public abstract class ScreenViewModel : ViewModel, IScreenViewModel
    {
        bool _isOpen;

        protected ScreenViewModel() {
            Activate = ReactiveCommand.Create().DefaultSetup("Activate");
            Close = ReactiveCommand.CreateAsyncTask(async x => x as bool?).DefaultSetup("Close");
            Help =
                ReactiveCommand.CreateAsyncTask(
                        async x => await this.Send(new OpenWebLink(ViewType.Help)).ConfigureAwait(false))
                    .DefaultSetup("Help");
        }

        public abstract string DisplayName { get; }
        public RoutingState Router { get; protected set; }
        public ReactiveCommand<object> Activate { get; }
        public ReactiveCommand<bool?> Close { get; }
        public bool IsOpen
        {
            get { return _isOpen; }
            set { this.RaiseAndSetIfChanged(ref _isOpen, value); }
        }
        public ICommand Help { get; }
    }
}