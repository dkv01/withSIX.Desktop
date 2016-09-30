// <copyright company="SIX Networks GmbH" file="ILibraryGroupDataGridView.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI;
using withSIX.Play.Applications.ViewModels.Games.Library.LibraryGroup;

namespace withSIX.Play.Applications.Views.Games.Library.LibraryGroup
{
    public interface ILibraryGroupDataGridView : IViewFor<LibraryGroupViewModel>,
        IModLibraryGroupView,
        IViewFor<MissionLibraryGroupViewModel>,
        IViewFor<ServerLibraryGroupViewModel> {}
}