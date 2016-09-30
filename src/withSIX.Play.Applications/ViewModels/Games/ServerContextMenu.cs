// <copyright company="SIX Networks GmbH" file="ServerContextMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Play.Applications.ViewModels.Games.Library;
using withSIX.Play.Core;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class ServerContextMenu : ContextMenuBase<Server>
    {
        readonly ServerLibraryViewModel _library;

        public ServerContextMenu(ServerLibraryViewModel library) {
            _library = library;
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Hexagon)]
        public void ActivateServer(Server entity) {
            _library.ActiveItem = entity;
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Add)]
        public void CreateCollectionWithServerMods(Server entity) {
            _library.CreateCollectionFromServer(entity);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Joystick)]
        public Task JoinServer(Server entity) => _library.JoinServer(entity);

        [MenuItem(Icon = SixIconFont.withSIX_icon_Hexagon)]
        public void CopyIpPortToClipboard(Server entity) {
            _library.CopyIpPortAction(entity);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Hexagon)]
        public void CopyDetailsToClipboard(Server entity) {
            _library.CopyDetailsAction(entity);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Lock)]
        public Task ChangeServerPassword(Server entity) => _library.ChangePassword(entity);

        [MenuItem(Icon = SixIconFont.withSIX_icon_Reload)]
        public Task RefreshServerInfo(Server entity) => _library.UpdateServer(entity);

        [MenuItem(Icon = SixIconFont.withSIX_icon_Hexagon_Info)]
        public void ShowServerInfo(Server entity) {
            _library.ServerInfo(entity);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_X)]
        public void ClearServerHistory(Server entity) {
            _library.ClearHistory(entity);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Notes)]
        public void ShowNotes(Server entity) {
            _library.ShowNotes(entity);
        }

        protected override void UpdateItemsFor(Server item) {
            base.UpdateItemsFor(item);

            GetItem(CreateCollectionWithServerMods)
                .IsVisible = DomainEvilGlobal.SelectedGame.ActiveGame.SupportsMods();
        }
    }
}