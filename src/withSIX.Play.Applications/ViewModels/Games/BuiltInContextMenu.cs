// <copyright company="SIX Networks GmbH" file="BuiltInContextMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>


using withSIX.Core.Applications.MVVM.Attributes;
using withSIX.Play.Applications.ViewModels.Games.Library;
using withSIX.Play.Core;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class BuiltInContextMenu : ModLibraryItemMenuBase<BuiltInContentContainer>
    {
        public BuiltInContextMenu(ModLibraryViewModel library) : base(library) {}

        [MenuItem]
        public void Clear(ContentLibraryItemViewModel<BuiltInContentContainer> item) {
            DomainEvilGlobal.Settings.ModOptions.RecentCollections.Clear();
        }

        protected override void UpdateItemsFor(ContentLibraryItemViewModel<BuiltInContentContainer> item) {
            GetItem(Clear)
                .IsVisible = item.Model.Name == "Recent"; // yuk
        }
    }
}