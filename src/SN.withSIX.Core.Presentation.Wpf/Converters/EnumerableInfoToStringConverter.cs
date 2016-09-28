// <copyright company="SIX Networks GmbH" file="EnumerableInfoToStringConverter.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace SN.withSIX.Core.Presentation.Wpf.Converters
{
    public class EnumerableInfoToStringConverter : IMultiValueConverter
    {
        static readonly string DefaultReturn = string.Empty;
        static readonly string defaultConcat = ", ";

        #region IValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if ((values == null) || (values.Length < 2) || (values[1] == null))
                return DefaultReturn;

            var collection = values[1] as IEnumerable<string>;
            if (collection == null)
                return DefaultReturn;

            if (parameter != null)
                return string.Format("{1} {0}: ", parameter, values[0]) + string.Join(defaultConcat, collection);
            return string.Join(defaultConcat, collection);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }
}