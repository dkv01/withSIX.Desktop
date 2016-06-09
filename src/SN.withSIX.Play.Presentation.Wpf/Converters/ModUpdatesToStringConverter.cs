// <copyright company="SIX Networks GmbH" file="ModUpdatesToStringConverter.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using SN.withSIX.Core;
using SN.withSIX.Play.Core.Games.Legacy.Mods;

namespace SN.withSIX.Play.Presentation.Wpf.Converters
{
    public class ModUpdatesToStringConverter : IValueConverter
    {
        static readonly string DefaultReturn = String.Empty;
        static readonly string Concat = "\n";

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null)
                return DefaultReturn;

            var collection = (IEnumerable<UpdateState>) value;
            return String.Join(Concat, collection
                .Select(mu =>
                    $"{mu.Mod.Name}: {mu.Revision} (Current: {mu.CurrentRevision ?? "None"}, {GetState(mu)}) Total Size: {Tools.FileUtil.GetFileSize(mu.SizeWd)}, Compressed: {Tools.FileUtil.GetFileSize(mu.Size)}"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        static string GetState(UpdateState mu) {
            if (string.IsNullOrWhiteSpace(mu.CurrentRevision))
                return "New";
            if (mu.IsEqual())
                return "Diagnose";
            return mu.IsNewer() ? "Upgrade" : "Downgrade";
        }

        #endregion
    }
}