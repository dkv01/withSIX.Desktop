// <copyright company="SIX Networks GmbH" file="CountryFlag.xaml.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

#region FileHeader

// withSIX withSIX.Core.Presentation.Wpf CountryFlagView.xaml.cs
// Copyright 2009-2013 SIX Networks GmbH
// Terms Of Service: http://www.withsix.com/tos

#endregion

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using withSIX.Core;
using withSIX.Core.Logging;
using withSIX.Core.Presentation.Wpf.Services;

namespace withSIX.Play.Presentation.Wpf.Views.Controls
{
    public partial class CountryFlagView : UserControl, IEnableLogging
    {
        public static readonly DependencyProperty FlagProperty = DependencyProperty.Register("Flag",
            typeof (CountryFlags),
            typeof (CountryFlagView), new FrameworkPropertyMetadata(ChangeFlag));
        static readonly string ResourcePath = ResourceService.ResourcePath + "/images/Flags/";

        public CountryFlagView() {
            InitializeComponent();
        }

        public CountryFlags Flag
        {
            get { return (CountryFlags) GetValue(FlagProperty); }
            set { SetValue(FlagProperty, value); }
        }

        async void HandleFlag(CountryFlags newValue) {
            var countryName = newValue.ToString();
            var path = ResourcePath + countryName + ".png";

            await TrySetFlagSource(path).ConfigureAwait(false);
        }

        async Task TrySetFlagSource(string path) {
            try {
                await SetFlagSource(path).ConfigureAwait(false);
            } catch (Exception e) {
                this.Logger().FormattedWarnException(e);
            }
        }

        async Task SetFlagSource(string path) {
            var img = await Cache.ImageFiles.BmiFromUriAsync(new Uri(path)).ConfigureAwait(false);
            await
                FlagImage.Dispatcher.InvokeAsync(new Action(() => FlagImage.Source = img), DispatcherPriority.DataBind);
        }

        static void ChangeFlag(DependencyObject source, DependencyPropertyChangedEventArgs e) {
            ((CountryFlagView) source).HandleFlag(((CountryFlags) e.NewValue));
        }
    }
}