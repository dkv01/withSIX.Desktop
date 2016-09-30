// <copyright company="SIX Networks GmbH" file="NewProfileView.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using ReactiveUI;

using SN.withSIX.Core.Presentation.Wpf.Views.Controls;
using withSIX.Play.Applications.ViewModels.Dialogs;
using withSIX.Play.Applications.Views.Dialogs;

namespace SN.withSIX.Play.Presentation.Wpf.Views.Dialogs
{
    
    public partial class NewProfileView : StandardDialog, INewProfileView
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof (INewProfileViewModel),
                typeof (NewProfileView),
                new PropertyMetadata(null));

        public NewProfileView() {
            InitializeComponent();
            this.WhenActivated(d => {
                d(this.WhenAnyValue(x => x.ViewModel).BindTo(this, v => v.DataContext));
                Name.Focus();
            });
        }

        public INewProfileViewModel ViewModel
        {
            get { return (INewProfileViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (INewProfileViewModel) value; }
        }
    }
}