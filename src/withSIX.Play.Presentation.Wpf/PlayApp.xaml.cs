// <copyright company="SIX Networks GmbH" file="PlayApp.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using System.Windows.Controls;
using withSIX.Core.Extensions;
using withSIX.Core.Presentation.Wpf;
using withSIX.Core.Presentation.Wpf.Services;

namespace withSIX.Play.Presentation.Wpf
{
    public partial class PlayApp : SingleInstanceApp
    {
        static PlayApp() {
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof (UIElement),
                new FrameworkPropertyMetadata(30.Seconds()));
        }

        public PlayApp() {
#if !DEBUG
            ExDialog.SetupExceptionHandler(this);
#endif
        }

        public static void Launch() {
            var application = new PlayApp();
            application.InitializeComponent();
            application.Run();
        }
    }
}