// <copyright company="SIX Networks GmbH" file="MissionLibraryItemContextMenuBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Glue.Helpers;

namespace withSIX.Play.Applications.ViewModels.Games.Library
{
    public abstract class MissionLibraryItemMenuBase<T> :
        ContentLibraryItemMenuBase<T, MissionLibraryViewModel> where T : class, ISelectionList<IContent>
    {
        protected MissionLibraryItemMenuBase(MissionLibraryViewModel library) : base(library) {}
    }
}