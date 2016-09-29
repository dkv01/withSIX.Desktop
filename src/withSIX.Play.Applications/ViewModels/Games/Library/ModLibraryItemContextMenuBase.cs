// <copyright company="SIX Networks GmbH" file="ModLibraryItemContextMenuBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Play.Core.Games.Legacy;
using SN.withSIX.Play.Core.Glue.Helpers;

namespace SN.withSIX.Play.Applications.ViewModels.Games.Library
{
    public abstract class ModLibraryItemMenuBase<T> : ContentLibraryItemMenuBase<T, ModLibraryViewModel>
        where T : class, ISelectionList<IContent>
    {
        protected ModLibraryItemMenuBase(ModLibraryViewModel library) : base(library) {}
    }
}