// <copyright company="SIX Networks GmbH" file="CollectionBarMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using withSIX.Play.Applications.ViewModels.Games.Library;
using withSIX.Play.Core.Games.Legacy.Mods;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class CollectionBarMenu : ContextMenuBase<Collection>
    {
        readonly ModLibraryViewModel _library;

        public CollectionBarMenu(ModLibraryViewModel library) {
            _library = library;

            this.WhenAnyValue(x => x.CurrentItem.IsInstalled)
                .Select(x => new {CurrentItem, x})
                .Subscribe(info => {
                    Items.Where(x => x.AsyncAction == Uninstall)
                        .ForEach(x => x.IsEnabled = info.x);
                });
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Hexagon_Info)]
        public void ShowInfo(Collection collection) {
            _library.ShowInfo(collection);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Tools)]
        public Task Diagnose(Collection collection) => _library.Diagnose(collection);

        [MenuItem(Icon = SixIconFont.withSIX_icon_Folder_Remove)]
        public Task Uninstall(Collection collection) => _library.Uninstall(collection);

        [MenuItem(Icon = SixIconFont.withSIX_icon_Folder_Remove)]
        public Task Remove(Collection collection) => _library.RemoveCollection(collection);
    }
}