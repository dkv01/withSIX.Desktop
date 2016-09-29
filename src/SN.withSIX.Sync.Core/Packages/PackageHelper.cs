// <copyright company="SIX Networks GmbH" file="PackageHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Text.RegularExpressions;

namespace withSIX.Sync.Core.Packages
{
    public static class PackageHelper
    {
        static readonly Regex PackifyRegex = new Regex("[^A-Za-z0-9-_]+", RegexOptions.Compiled);

        public static string Packify(string text) => PackifyRegex.Replace(GetBaseName(text), PackifyMatchString)
            .Trim()
            .Trim('-', '_');

        static string GetBaseName(string text) => text.ToLower().Replace("%20", "_");

        static string PackifyMatchString(Match match) {
            if (!match.Success)
                return string.Empty;

            switch (match.Value) {
            case " ": {
                return "_";
            }
            case "'": {
                return "";
            }
            case "\"": {
                return "";
            }
            default: {
                return "-";
            }
            }
        }
    }
}