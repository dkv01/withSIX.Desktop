// <copyright company="SIX Networks GmbH" file="AdvancedCollectionView.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Windows;
using System.Windows.Controls;
using ReactiveUI;
using withSIX.Play.Applications.Views.Games.Library;

namespace withSIX.Play.Presentation.Wpf.Views.Games.Library
{
    /// <summary>
    ///     Interaction logic for AdvancedCollectionView.xaml
    /// </summary>
    public partial class AdvancedCollectionView : UserControl, IAdvancedCollectionView
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof (AdvancedCollectionViewModel),
                typeof (AdvancedCollectionView),
                new PropertyMetadata(null));

        public AdvancedCollectionView() {
            InitializeComponent();
        }

        public AdvancedCollectionViewModel ViewModel
        {
            get { return (AdvancedCollectionViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as AdvancedCollectionViewModel; }
        }
    }
}