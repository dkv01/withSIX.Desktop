// <copyright company="SIX Networks GmbH" file="IHaveSelectedItem.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Core.Applications.MVVM.Helpers
{
    public interface IHaveSelectedItem<T>
    {
        T SelectedItem { get; set; }
    }
}