// <copyright company="SIX Networks GmbH" file="BasicUserErrorHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Windows;
using ReactiveUI;

namespace withSIX.Core.Presentation.Wpf.Services
{
    public static class BasicUserErrorHandler
    {
        public static IDisposable RegisterDefaultHandler(this Window window)
            => UserError.RegisterHandler(error => UiRoot.Main.ErrorHandler.Handler(error, window));
    }
}