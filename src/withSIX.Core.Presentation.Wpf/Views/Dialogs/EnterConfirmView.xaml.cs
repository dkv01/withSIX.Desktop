// <copyright company="SIX Networks GmbH" file="EnterConfirmView.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using System.Windows.Navigation;
using ReactiveUI;
using withSIX.Core.Applications.MVVM.ViewModels.Dialogs;
using withSIX.Core.Applications.MVVM.Views.Dialogs;
using withSIX.Core.Presentation.Wpf.Views.Controls;

namespace withSIX.Core.Presentation.Wpf.Views.Dialogs
{
    public partial class EnterConfirmView : StandardDialog, IEnterConfirmView
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(IEnterConfirmViewModel), typeof(EnterConfirmView),
                new PropertyMetadata(null));

        public EnterConfirmView() {
            InitializeComponent();
            this.WhenActivated(d => { d(this.WhenAnyValue(x => x.ViewModel).BindTo(this, v => v.DataContext)); });
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as IEnterConfirmViewModel; }
        }
        public IEnterConfirmViewModel ViewModel
        {
            get { return (IEnterConfirmViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        void hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            if ((e.Uri != null) && (string.IsNullOrEmpty(e.Uri.OriginalString) == false)) {
                Tools.Generic.TryOpenUrl(e.Uri);
                e.Handled = true;
            }
        }
    }
}