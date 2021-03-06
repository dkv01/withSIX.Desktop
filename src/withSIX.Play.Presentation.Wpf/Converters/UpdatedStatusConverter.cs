// <copyright company="SIX Networks GmbH" file="UpdatedStatusConverter.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Globalization;
using System.Windows.Data;

namespace withSIX.Play.Presentation.Wpf.Converters
{
    public class UpdatedStatusConverter : IValueConverter
    {
        static readonly string DefaultReturn = "Black";
        static readonly string[] units = {"Gray", DefaultReturn, "Orange"};

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null)
                return DefaultReturn;
            var size = (int) value;
            if (size < 0 || size >= units.Length)
                return DefaultReturn;

            return units[size];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }
}