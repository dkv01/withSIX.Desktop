// <copyright company="SIX Networks GmbH" file="IShellViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using Caliburn.Micro;
using ReactiveUI.Legacy;


namespace SN.withSIX.Core.Applications.MVVM.ViewModels
{
    
    public interface IShellViewModel : IScreen {}

    public interface IShellViewModelTrayIcon : IScreen
    {
        ContextMenuBase TrayIconContextMenu { get; }
        ReactiveCommand TrayIconDoubleclicked { get; }
    }
}