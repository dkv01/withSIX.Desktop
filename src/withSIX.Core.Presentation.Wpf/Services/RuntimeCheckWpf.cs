// <copyright company="SIX Networks GmbH" file="RuntimeCheckWpf.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using System.Windows;
using withSIX.Core.Presentation.Bridge;

namespace withSIX.Core.Presentation.Wpf.Services
{
    public class RuntimeCheckWpf : RuntimeCheck
    {
        protected override async Task<bool> FatalErrorMessage(string message, string caption)
            => MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Exclamation,
                   MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes;
    }
}