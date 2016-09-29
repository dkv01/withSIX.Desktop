// <copyright company="SIX Networks GmbH" file="MessageBoxView.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using ReactiveUI;
using withSIX.Core.Applications.MVVM.ViewModels.Dialogs;
using withSIX.Core.Applications.MVVM.Views.Dialogs;
using withSIX.Core.Presentation.Wpf.Views.Controls;

namespace withSIX.Core.Presentation.Wpf.Views.Dialogs
{
    public partial class MessageBoxView : StandardDialog, IMessageBoxView
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(IMessageBoxViewModel), typeof(MessageBoxView),
                new PropertyMetadata(null));

        public MessageBoxView() {
            InitializeComponent();
            this.WhenActivated(d => { d(this.WhenAnyValue(x => x.ViewModel).BindTo(this, v => v.DataContext)); });
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as IMessageBoxViewModel; }
        }
        public IMessageBoxViewModel ViewModel
        {
            get { return (IMessageBoxViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}