﻿// <copyright company="SIX Networks GmbH" file="CollectionCreatedView.xaml.cs">
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
    
    public partial class CollectionCreatedView : StandardDialog, ICollectionCreatedView
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof (ICollectionCreatedViewModel),
                typeof (CollectionCreatedView),
                new PropertyMetadata(null));

        public CollectionCreatedView() {
            InitializeComponent();
            this.WhenActivated(d => { d(this.WhenAnyValue(x => x.ViewModel).BindTo(this, v => v.DataContext)); });
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as ICollectionCreatedViewModel; }
        }
        public ICollectionCreatedViewModel ViewModel
        {
            get { return (ICollectionCreatedViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}