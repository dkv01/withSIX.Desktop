// <copyright company="SIX Networks GmbH" file="TrayIconContextMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using withSIX.Core.Applications;
using withSIX.Core.Applications.MVVM.Attributes;
using withSIX.Core.Applications.MVVM.Extensions;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Extensions;
using withSIX.Play.Core;
using withSIX.Play.Core.Options.Entries;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class RecentMenuItem : MenuItem
    {
        public RecentMenuItem(RecentCollection collection) {
            Id = collection.Id;
            Name = collection.Name;
            Action = () => Process.Start(collection.GetLaunchUrl());
        }

        public Guid Id { get; }
    }

    public class RecentContextMenu : MenuItem
    {
        public RecentContextMenu() {
            Name = "Recent";
            Icon = SixIconFont.withSIX_icon_Clock;

            DomainEvilGlobal.Settings.ModOptions.RecentCollections.CopyAndTrackChangesOnUiThread(
                AddRecentItem,
                RemoveRecentItem,
                reset => {
                    Items.Clear();
                    reset.ForEach(AddRecentItem);
                });
        }

        void AddRecentItem(RecentCollection collection) {
            RemoveRecentItem(collection);
            InsertRecentItem(collection);

            if (Items.Count > 10)
                Items.RemoveAt(10);
        }

        void InsertRecentItem(RecentCollection collection) {
            Items.Insert(0, new RecentMenuItem(collection));
        }

        void RemoveRecentItem(RecentCollection collection) {
            Items.RemoveAll<IMenuItem, RecentMenuItem>(x => x.Id == collection.Id);
        }
    }

    public class TrayIconContextMenu : ContextMenuBase
    {
        readonly IPlayShellViewModel _psvm;

        public TrayIconContextMenu(IPlayShellViewModel playShellViewModel) {
            _psvm = playShellViewModel;

            Items.InsertRange(0, new[] {
                new RecentContextMenu(),
                new MenuItem {IsSeparator = true}
            });
        }

        [MenuItem]
        public void Open() {
            _psvm.TrayIconDoubleclicked.Execute(null);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_X)]
        public void Exit() {
            _psvm.Exit.Execute(null);
        }
    }
}