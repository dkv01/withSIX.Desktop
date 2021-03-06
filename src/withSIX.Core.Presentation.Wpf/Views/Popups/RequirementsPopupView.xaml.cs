﻿// <copyright company="SIX Networks GmbH" file="RequirementsPopupView.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using ReactiveUI;
using withSIX.Core.Applications.MVVM.ViewModels.Popups;
using withSIX.Core.Presentation.Wpf.Views.Controls;

namespace withSIX.Core.Presentation.Wpf.Views.Popups
{
    /// <summary>
    ///     Interaction logic for RequirementsPopupView.xaml
    /// </summary>
    public partial class RequirementsPopupView : PopupControl, IViewFor<RequirementsPopupViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(RequirementsPopupViewModel), typeof(RequirementsPopupView),
                new PropertyMetadata(null));

        public RequirementsPopupView() {
            InitializeComponent();
            this.WhenActivated(d => { d(this.WhenAnyValue(x => x.ViewModel).BindTo(this, v => v.DataContext)); });
            //Loaded += (sender, args) => LayoutRoot.Focus();
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as RequirementsPopupViewModel; }
        }
        public RequirementsPopupViewModel ViewModel
        {
            get { return (RequirementsPopupViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}