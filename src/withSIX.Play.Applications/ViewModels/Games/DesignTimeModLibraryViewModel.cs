// <copyright company="SIX Networks GmbH" file="DesignTimeModLibraryViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Core.Applications;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Play.Applications.ViewModels.Games.Library;
using withSIX.Play.Applications.ViewModels.Games.Library.LibraryGroup;
using withSIX.Play.Core.Games.Entities.RealVirtuality;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Play.Core.Options.Entries;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class DesignTimeModLibraryViewModel : ModLibraryViewModel, IDesignTimeViewModel
    {
        public DesignTimeModLibraryViewModel() {
            var game = new Arma1Game(Guid.NewGuid(), new GameSettingsController());
            var collectionGroup = new ModLibraryGroupViewModel(this, "Collections",
                icon: SixIconFont.withSIX_icon_Folder);
            var collections =
                new CustomCollectionLibraryItemViewModel(this,
                    new CustomCollection(Guid.NewGuid(), game) {Name = "Test ModSet"},
                    collectionGroup);
            var subscribedCollections =
                new SubscribedCollectionLibraryItemViewModel(this,
                    new SubscribedCollection(Guid.NewGuid(), Guid.NewGuid(), game) {Name = "Test ModSet3"},
                    collectionGroup);
            var mod = new Mod(Guid.Empty) {
                Name = "@TESTMOD",
                FullName = "Da Full name fewafefiejaofijeafoijeafo ieafioj eaoi",
                Author = "The Author",
                Version = "1.2.0"
            };
            collections.Items.Add(new CustomCollection(Guid.NewGuid(), game) {
                Name = "Some ModSet wuith faopek faof aepokf poaefpokpof  eaf",
                Author = "Some author",
                Version = "1.0.2"
            });
            collections.Items.Add(mod);
            collections.SelectedItem = mod;

            // TODO
            //CreateItemsView(
            //    new ReactiveList<ContentLibraryItem>(new[]
            //    {subscribedCollections, sharedCollections, localCollections}), new LibraryGroup[0]);
            SelectedItem = collections;
        }
    }
}