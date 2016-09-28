// <copyright company="SIX Networks GmbH" file="UiRoot.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Core.Presentation.Wpf
{
    public class UiRoot : IPresentationService
    {
        public UiRoot(IDialogManager dialogManager, ISpecialDialogManager specialDialogManager) {
            ErrorHandler = new WpfErrorHandler(dialogManager, specialDialogManager);
        }

        public WpfErrorHandler ErrorHandler { get; }
        public static UiRoot Main { get; set; }
    }
}