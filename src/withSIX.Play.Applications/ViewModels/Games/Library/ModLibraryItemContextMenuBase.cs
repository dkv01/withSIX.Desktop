// <copyright company="SIX Networks GmbH" file="ModLibraryItemContextMenuBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Glue.Helpers;

namespace withSIX.Play.Applications.ViewModels.Games.Library
{
    public abstract class ModLibraryItemMenuBase<T> : ContentLibraryItemMenuBase<T, ModLibraryViewModel>
        where T : class, ISelectionList<IContent>
    {
        protected ModLibraryItemMenuBase(ModLibraryViewModel library) : base(library) {}
    }
}