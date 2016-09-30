// <copyright company="SIX Networks GmbH" file="LocalModFolderContextMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using withSIX.Play.Applications.ViewModels.Games.Library;
using withSIX.Play.Core.Games.Legacy;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class LocalModFolderContextMenu : ModLibraryItemMenuBase<LocalModsContainer>
    {
        public LocalModFolderContextMenu(ModLibraryViewModel library) : base(library) {
            Contract.Requires<ArgumentNullException>(library != null);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Folder)]
        public void ShowInExplorer(ContentLibraryItemViewModel<LocalModsContainer> contentItem) {
            Tools.FileUtil.OpenFolderInExplorer(contentItem.Model.Path);
        }

        [MenuItem]
        public void EditLocation(ContentLibraryItemViewModel<LocalModsContainer> contentItem) {
            Library.MoveLocalModDirectory(contentItem);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_X)]
        public Task RemoveLocation(ContentLibraryItemViewModel contentItem) => contentItem.Remove();

        protected override void UpdateItemsFor(ContentLibraryItemViewModel<LocalModsContainer> lm) {
            GetAsyncItem(RemoveLocation)
                .IsEnabled = !lm.IsFeatured;

            GetItem(EditLocation)
                .IsEnabled = lm.IsFeatured;
        }
    }
}