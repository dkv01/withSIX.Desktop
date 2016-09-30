// <copyright company="SIX Networks GmbH" file="ContentLibraryRootViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Play.Core.Games.Legacy;

namespace withSIX.Play.Applications.ViewModels.Games.Library
{
    public abstract class ContentLibraryRootViewModel :
        LibraryRootViewModel<ContentLibraryItemViewModel, IContent, SearchContentLibraryItemViewModel>
    {
        readonly ModuleViewModelBase _module;

        protected ContentLibraryRootViewModel(ModuleViewModelBase module) {
            _module = module;
        }
    }
}