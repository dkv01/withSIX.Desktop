// <copyright company="SIX Networks GmbH" file="MissionContextMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

using SN.withSIX.Core.Applications;
using SN.withSIX.Core.Applications.MVVM.Attributes;
using SN.withSIX.Core.Applications.MVVM.ViewModels;
using SN.withSIX.Play.Applications.ViewModels.Games.Library;
using SN.withSIX.Play.Core.Games.Legacy;
using SN.withSIX.Play.Core.Games.Legacy.Missions;
using SN.withSIX.Play.Core.Games.Legacy.Mods;

namespace SN.withSIX.Play.Applications.ViewModels.Games
{
    public abstract class MissionMenuBase : ContextMenuBase<IContent>
    {
        public MissionLibraryViewModel Library { get; }

        protected MissionMenuBase(MissionLibraryViewModel library) {
            Contract.Requires<ArgumentNullException>(library != null);
            Library = library;
        }
    }

    public class MissionContextMenu : MissionMenuBase
    {
        readonly object _updateLock = new object();
        volatile IContent _target;
        public MissionContextMenu(MissionLibraryViewModel library) : base(library) {}

        [MenuItem(Icon = SixIconFont.withSIX_icon_Hexagon_Info)]
        public void ShowInfo(IContent mission) {
            Library.ShowInfo(mission);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Download)]
        public Task Install(IContent mission) => Library.DownloadMission((Mission)mission);

        [MenuItem(Icon = SixIconFont.withSIX_icon_Joystick)]
        public async Task Launch(IContent content) {
            var mission = (Mission) content;
            if (!mission.IsLocal) {
                await Library.DownloadMission(mission).ConfigureAwait(false);
                // TODO: This should be removed once the main button operates properly
            }

            await Library.LaunchMission(mission).ConfigureAwait(false);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Upload)]
        public Task Publish(IContent mission) => Library.PublishMission((Mission)mission);

        [MenuItem(Icon = SixIconFont.withSIX_icon_Folder)]
        public void OpenInExplorer(IContent mission) {
            ((MissionBase) mission).OpenInExplorer();
        }

        protected override void UpdateItemsFor(IContent item) {
            lock (_updateLock)
                UpdateForItemInternal2(item);
        }

        void UpdateForItemInternal2(IContent item) {
            _target = item;
            var mission = (Mission) item;

            var installAction = GetAsyncItem(Install);
            installAction.IsVisible = !mission.IsLocal;
            installAction.Name = ModController.ConvertState(mission.State);

            GetAsyncItem(Publish)
                .IsVisible = mission.IsLocal;


            var hasPackageName = !string.IsNullOrWhiteSpace(mission.PackageName);
            var controller = mission.Controller;
            var packageItem = controller.Package;
            var hasPackage = packageItem != null && packageItem.Current != null;

            GetItem(ShowInfo)
                .IsVisible = hasPackageName;

            GetItem(OpenInExplorer)
                .IsVisible = mission.IsLocal || mission.Controller.IsInstalled;
        }
    }

    public class MissionFolderContextMenu : MissionMenuBase
    {
        public MissionFolderContextMenu(MissionLibraryViewModel library) : base(library) {}

        [MenuItem(Icon = SixIconFont.withSIX_icon_Hexagon_Info)]
        public void ShowInfo(IContent mission) {
            Library.ShowInfo(mission);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Rocket)]
        public Task LaunchGameEditor(IContent mission) => Library.OpenMissionInGameEditor((MissionFolder)mission);

        [MenuItem(Icon = SixIconFont.withSIX_icon_Upload)]
        public Task Publish(IContent mission) => Library.PublishMission((MissionFolder)mission);

        [MenuItem(Icon = SixIconFont.withSIX_icon_Folder)]
        public void OpenInExplorer(IContent mission) {
            ((MissionBase) mission).OpenInExplorer();
        }

        protected override void UpdateItemsFor(IContent item) {
            var mission = (MissionFolder) item;

            GetItem(ShowInfo)
                .IsVisible = !mission.IsLocal;

            GetItem(OpenInExplorer)
                .IsVisible = mission.IsLocal || mission.Controller.IsInstalled;
        }
    }
}