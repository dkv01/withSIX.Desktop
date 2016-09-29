// <copyright company="SIX Networks GmbH" file="RequirementsPopupViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using ReactiveUI.Legacy;

namespace withSIX.Core.Applications.MVVM.ViewModels.Popups
{
    public class RequirementsPopupViewModel : PopupBase
    {
        IObservable<object> _command;
        string _commandTitle;
        string _message;

        public RequirementsPopupViewModel(ReactiveCommand toCommand) {
            Command = toCommand;
            Command.Subscribe(x => TryClose());
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }
        public string CommandTitle
        {
            get { return _commandTitle; }
            set { SetProperty(ref _commandTitle, value); }
        }
        public IObservable<object> Command
        {
            get { return _command; }
            set { SetProperty(ref _command, value); }
        }
    }
}