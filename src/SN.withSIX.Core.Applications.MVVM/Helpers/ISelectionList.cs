// <copyright company="SIX Networks GmbH" file="ISelectionList.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Core.Applications.MVVM.Helpers
{
    public interface ISelectionList<T> : IHaveReactiveItems<T>, IHaveSelectedItem<T> where T : class {}
}