// <copyright company="SIX Networks GmbH" file="IHaveSelectedItem.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Play.Core.Glue.Helpers
{
    public interface IHaveSelectedItem<T>
    {
        T SelectedItem { get; set; }
    }
}