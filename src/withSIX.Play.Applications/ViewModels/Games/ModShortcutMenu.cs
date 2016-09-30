// <copyright company="SIX Networks GmbH" file="ModShortcutMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

using withSIX.Core.Applications.MVVM.Attributes;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Play.Applications.ViewModels.Games.Library;
using withSIX.Play.Core;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class ModShortcutMenu : MenuItem<CollectionLibraryItemViewModel>
    {
        public ModShortcutMenu(object parent) {
            Contract.Requires<ArgumentNullException>(parent != null);
            Parent = (CustomCollectionContextMenu) parent;
        }

        public CustomCollectionContextMenu Parent { get; set; }

        [MenuItem]
        public Task CreateDesktopShortcut(CollectionLibraryItemViewModel content) => Parent.Library.CreateShortcutGame(content.Model);

        [MenuItem("Create desktop shortcut through PwS; Update and Launch")]
        public Task CreateDesktopShortcutThroughPws(CollectionLibraryItemViewModel content) => Parent.Library.CreateShortcutPws(content.Model);

        [MenuItem("Create desktop shortcut through PwS; Update and Join")]
        public Task CreateDesktopShortcutThroughPwsJoin(CollectionLibraryItemViewModel content) => Parent.Library.CreateShortcutPwsJoin(content.Model);

        [MenuItem("Create desktop shortcut through PwS in lockdown")]
        public Task CreateDesktopShortcutThroughPwsLockdown(CollectionLibraryItemViewModel content) => Parent.Library.CreateShortcutPwsLockdown(content.Model);

        protected override void UpdateItemsFor(CollectionLibraryItemViewModel item) {
            base.UpdateItemsFor(item);

            GetAsyncItem(CreateDesktopShortcut)
                .IsEnabled = DomainEvilGlobal.SelectedGame.ActiveGame.InstalledState.IsInstalled;
        }
    }
}