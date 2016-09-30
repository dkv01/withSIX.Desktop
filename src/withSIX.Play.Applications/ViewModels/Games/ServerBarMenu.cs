// <copyright company="SIX Networks GmbH" file="ServerBarMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Core.Applications;
using withSIX.Core.Applications.MVVM.Attributes;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Play.Applications.ViewModels.Games.Library;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class ServerBarMenu : ContextMenuBase<Server>
    {
        readonly ServerLibraryViewModel _library;

        public ServerBarMenu(ServerLibraryViewModel library) {
            _library = library;
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Hexagon_Info)]
        public void ShowInfo(Server server) {
            _library.ServerInfo(server);
        }

        [MenuItem(Icon = SixIconFont.withSIX_icon_Joystick)]
        public Task Join(Server server) => _library.JoinServer(server);
    }
}