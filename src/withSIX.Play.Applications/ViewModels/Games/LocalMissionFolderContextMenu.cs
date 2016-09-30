// <copyright company="SIX Networks GmbH" file="LocalMissionFolderContextMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using withSIX.Play.Applications.ViewModels.Games.Library;
using withSIX.Play.Core.Games.Legacy;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class LocalMissionFolderContextMenu : MissionLibraryItemMenuBase<LocalMissionsContainer>
    {
        public LocalMissionFolderContextMenu(MissionLibraryViewModel library) : base(library) {
            Contract.Requires<ArgumentNullException>(library != null);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Folder)]
        public void ShowInExplorer(ContentLibraryItemViewModel<LocalMissionsContainer> contentItem) {
            Tools.FileUtil.OpenFolderInExplorer(contentItem.Model.Path);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_X)]
        public Task RemoveLocalFolder(ContentLibraryItemViewModel<LocalMissionsContainer> contentItem) => contentItem.Remove();

        [MenuItem]
        public void Move(ContentLibraryItemViewModel<LocalMissionsContainer> contentItem) {
            Library.MoveLocalMissionDirectory(contentItem);
        }

        protected override void UpdateItemsFor(ContentLibraryItemViewModel<LocalMissionsContainer> lm) {
            GetAsyncItem(RemoveLocalFolder)
                .IsEnabled = !lm.IsFeatured;

            GetItem(Move)
                .IsEnabled = lm.IsFeatured;
        }
    }
}