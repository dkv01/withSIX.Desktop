// <copyright company="SIX Networks GmbH" file="ViewType.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.ComponentModel;


namespace withSIX.Play.Core.Options
{

    public enum ViewType
    {
        [Description("Card view")] List,
        [Description("Data view")] Grid
    }

    public static class ViewTypeString
    {
        public const string List = "List";
        public const string Grid = "Grid";
    }
}