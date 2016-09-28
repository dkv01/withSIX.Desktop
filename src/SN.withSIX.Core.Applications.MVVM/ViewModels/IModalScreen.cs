// <copyright company="SIX Networks GmbH" file="IModalScreen.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using Caliburn.Micro;

namespace SN.withSIX.Core.Applications.MVVM.ViewModels
{
    public interface IShowBackButton
    {
        bool ShowBackButton { get; }
    }


    public interface IModalScreen : IScreen, IShowBackButton
    {
        object Parent { get; set; }
        void Cancel();
    }
}