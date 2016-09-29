// <copyright company="SIX Networks GmbH" file="WpfPlatformProvider.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Presentation;
using withSIX.Mini.Applications.MVVM.Services;

namespace withSIX.Mini.Presentation.Wpf.Services
{
    public class WpfPlatformProvider : IPlatformProvider, IPresentationService
    {
        public bool InDesignMode => UiConstants.InDesignMode;
    }
}