// <copyright company="SIX Networks GmbH" file="ContentStateToButtonActionConverter.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Globalization;
using System.Windows.Data;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Mods;

namespace withSIX.Play.Presentation.Wpf.Converters
{
    public class ContentStateToButtonActionConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value == null ? null : ModController.ConvertState((ContentState)value);


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }
}