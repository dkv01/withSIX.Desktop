﻿// <copyright company="SIX Networks GmbH" file="RecentJumpList.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Windows;
using System.Windows.Shell;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Applications.MVVM;
using withSIX.Core.Applications.MVVM.Extensions;
using withSIX.Core.Extensions;
using withSIX.Play.Core;
using withSIX.Play.Core.Options.Entries;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class RecentJumpTask : JumpTask
    {
        public RecentJumpTask(RecentCollection collection) {
            Id = collection.Id;
            Title = collection.Name;
            Description = "Launch: " + collection.Name;
            CustomCategory = "Recent";
            ApplicationPath = collection.GetLaunchUrl();
            IconResourcePath = Common.Paths.EntryLocation.ToString();
        }

        public Guid Id { get; }
    }

    public class RecentJumpList
    {
        readonly JumpList _jumpList = new JumpList();

        public RecentJumpList() {
            DomainEvilGlobal.Settings.ModOptions.RecentCollections.CopyAndTrackChangesOnUiThread(
                AddRecentItem,
                x => _jumpList.JumpItems.RemoveAll<JumpItem, RecentJumpTask>(y => y.Id == x.Id),
                reset => {
                    _jumpList.JumpItems.Clear();
                    reset.ForEach(AddRecentItem);
                }
                );

            JumpList.SetJumpList(Application.Current, _jumpList);
        }

        void AddRecentItem(RecentCollection collection) {
            _jumpList.JumpItems.RemoveAll<JumpItem, RecentJumpTask>(x => x.Id == collection.Id);
            _jumpList.JumpItems.Insert(0, new RecentJumpTask(collection));

            if (_jumpList.JumpItems.Count > 10)
                _jumpList.JumpItems.RemoveAt(10);

            UiHelper.TryOnUiThread(_jumpList.Apply);
        }
    }
}