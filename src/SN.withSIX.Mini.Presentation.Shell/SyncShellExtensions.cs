// <copyright company="SIX Networks GmbH" file="SyncShellExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

namespace SN.withSIX.Mini.Presentation.Shell
{
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.Directory)]
    public class SyncShellExtension : SharpContextMenu
    {
        private readonly Helper _helper;

        public SyncShellExtension() {
            _helper = new Helper();
        }

        /// <summary>
        ///     Determines whether this instance can a shell
        ///     context show menu, given the specified selected file list.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance should show a shell context
        ///     menu for the specified file list; otherwise, <c>false</c>.
        /// </returns>
        protected override bool CanShowMenu() {
            var folderPaths = SelectedItemPaths.ToList();
            var result = _helper.TryGetInfo(folderPaths).Result;
            return result.Count == folderPaths.Count;
        }

        /// <summary>
        ///     Creates the context menu. This can be a single menu item or a tree of them.
        /// </summary>
        /// <returns>
        ///     The context menu for the shell context menu.
        /// </returns>
        protected override ContextMenuStrip CreateMenu() {
            //  Create the menu strip.
            var menu = new ContextMenuStrip();

            BuildSync(menu);

            //  Return the menu.
            return menu;
        }

        void BuildSync(ToolStrip menu) {
            var itemCountLines = new ToolStripMenuItem {
                Text = "Sync to withSIX",
                Image = _helper.LoadImage()
            };
            itemCountLines.Click += (sender, args) => TrySync();
            menu.Items.Add(itemCountLines);
        }

        async void TrySync() {
            await _helper.Try(Sync, "Sync").ConfigureAwait(false);
        }

        async Task Sync() {
            await _helper.HandleSyncRunning().ConfigureAwait(false);

            var folderPaths = SelectedItemPaths.ToList();
            var result = _helper.TryGetInfo(folderPaths).Result;

            foreach (var r in result) {
                _helper.OpenUrl("http://withsix.com/p/Arma-3/mods/" + new ShortGuid(r.ContentInfo.ContentId) +
                                "?upload=1");
            }
        }
    }
}