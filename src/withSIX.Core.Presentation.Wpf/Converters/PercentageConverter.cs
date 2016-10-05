// <copyright company="SIX Networks GmbH" file="PercentageConverter.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Globalization;
using System.Windows.Data;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;

namespace withSIX.Core.Presentation.Wpf.Converters
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            CultureInfo culture) {
            var dVal = (double) value;
            var dParam = ((string) parameter).TryDouble();
            return dVal*dParam;
        }

        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}