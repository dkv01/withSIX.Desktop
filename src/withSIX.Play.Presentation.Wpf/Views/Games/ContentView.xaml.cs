﻿// <copyright company="SIX Networks GmbH" file="ContentView.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using System.Windows.Controls;
using ReactiveUI;

using withSIX.Play.Applications.ViewModels.Games;
using withSIX.Play.Applications.Views.Games;

namespace withSIX.Play.Presentation.Wpf.Views.Games
{
    /// <summary>
    ///     Interaction logic for ContentView.xaml
    /// </summary>
    
    public partial class ContentView : UserControl, IContentView
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof (IContentViewModel), typeof (ContentView),
                new PropertyMetadata(null));

        public ContentView() {
            InitializeComponent();

            this.WhenActivated(d => { d(this.WhenAnyValue(x => x.ViewModel).BindTo(this, v => v.DataContext)); });
        }

        public IContentViewModel ViewModel
        {
            get { return (IContentViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (IContentViewModel) value; }
        }
    }
}