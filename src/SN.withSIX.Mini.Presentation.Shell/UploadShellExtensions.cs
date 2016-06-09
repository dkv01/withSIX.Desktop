// <copyright company="SIX Networks GmbH" file="UploadShellExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;

namespace SN.withSIX.Mini.Presentation.Shell
{
    // Unregister with /unregister respectively
    // Caveats:
    // Needs restart of explorer.exe
    // Probably should be installed to a global path, and then only updated if the md5 of the dll doesnt match.
    // in that case, kill explorer.exe, unregister, update, register, restart explorer...
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.Directory)]
    public class UploadShellExtension : SharpContextMenu
    {
        private readonly Helper _helper;

        public UploadShellExtension() {
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
            return result.Count == 0;
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

            BuildUpload(menu);

            //  Return the menu.
            return menu;
        }

        void BuildUpload(ToolStrip menu) {
            var itemCountLines = new ToolStripMenuItem {
                Text = "Upload to withSIX",
                Image = _helper.LoadImage()
            };
            itemCountLines.Click += (sender, args) => TryUpload();
            menu.Items.Add(itemCountLines);
        }

        async void TryUpload() {
            await _helper.Try(Upload, "Upload").ConfigureAwait(false);
        }

        async Task Upload() {
            await _helper.HandleSyncRunning().ConfigureAwait(false);

            var itemPaths = SelectedItemPaths.ToList();
            await _helper.WhitelistPaths(itemPaths).ConfigureAwait(false);

            foreach (var r in itemPaths) {
                // TODO: Game selection?
                _helper.OpenUrl("http://withsix.com/p/Arma-3/mods/?upload=1&folder=" + Uri.EscapeUriString(r));
            }
        }
    }
}