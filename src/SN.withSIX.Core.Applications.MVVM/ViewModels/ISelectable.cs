// <copyright company="SIX Networks GmbH" file="ISelectable.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI;

namespace SN.withSIX.Core.Applications.MVVM.ViewModels
{
    public interface ISelectable
    {
        bool IsSelected { get; set; }
    }

    public interface IRxClose
    {
        ReactiveCommand<bool?> Close { get; set; }
    }

}