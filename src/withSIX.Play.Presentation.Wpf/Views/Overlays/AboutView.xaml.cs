// <copyright company="SIX Networks GmbH" file="AboutView.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Windows.Controls;
using System.Windows.Documents;

using withSIX.Play.Core.Connect;

namespace withSIX.Play.Presentation.Wpf.Views.Overlays
{
    
    public partial class AboutView : UserControl
    {
        public AboutView() {
            InitializeComponent();
        }

        public void HyperlinkClicked(object obj, EventArgs e) {
            var hl = (Hyperlink) obj;
            BrowserHelper.TryOpenUrlIntegrated(hl.NavigateUri);
        }
    }
}