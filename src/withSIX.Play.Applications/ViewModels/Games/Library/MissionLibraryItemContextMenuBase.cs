// <copyright company="SIX Networks GmbH" file="MissionLibraryItemContextMenuBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Play.Core.Games.Legacy;
using SN.withSIX.Play.Core.Glue.Helpers;

namespace SN.withSIX.Play.Applications.ViewModels.Games.Library
{
    public abstract class MissionLibraryItemMenuBase<T> :
        ContentLibraryItemMenuBase<T, MissionLibraryViewModel> where T : class, ISelectionList<IContent>
    {
        protected MissionLibraryItemMenuBase(MissionLibraryViewModel library) : base(library) {}
    }
}