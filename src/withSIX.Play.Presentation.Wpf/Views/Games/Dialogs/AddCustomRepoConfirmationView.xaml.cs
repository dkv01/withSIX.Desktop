﻿// <copyright company="SIX Networks GmbH" file="AddCustomRepoConfirmationView.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using ReactiveUI;

using withSIX.Core.Presentation.Wpf.Views.Controls;
using withSIX.Play.Applications.ViewModels.Games.Dialogs;
using withSIX.Play.Applications.Views.Games.Dialogs;

namespace withSIX.Play.Presentation.Wpf.Views.Games.Dialogs
{
    /// <summary>
    ///     Interaction logic for CollectionCreatedDialog.xaml
    /// </summary>
    
    public partial class AddCustomRepoConfirmationView : StandardDialog, IAddCustomRepoConfirmationView
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof (IAddCustomRepoConfirmationViewModel),
                typeof (AddCustomRepoConfirmationView),
                new PropertyMetadata(null));

        public AddCustomRepoConfirmationView() {
            InitializeComponent();
            this.WhenActivated(d => { d(this.WhenAnyValue(x => x.ViewModel).BindTo(this, v => v.DataContext)); });
        }

        public IAddCustomRepoConfirmationViewModel ViewModel
        {
            get { return (IAddCustomRepoConfirmationViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (IAddCustomRepoConfirmationViewModel) value; }
        }
    }
}