// <copyright company="SIX Networks GmbH" file="IHaveReactiveItems.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI;
using withSIX.Core.Helpers;

namespace withSIX.Core.Applications.MVVM.Helpers
{
    public interface IHaveReactiveItems<T> : IHaveItems<T, ReactiveList<T>> {}
}