// <copyright company="SIX Networks GmbH" file="ISelectionList.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Play.Core.Glue.Helpers
{
    public interface ISelectionList<T> : IHaveReactiveItems<T>, IHaveSelectedItem<T> where T : class {}
}