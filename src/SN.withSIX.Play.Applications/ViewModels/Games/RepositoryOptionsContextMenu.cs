// <copyright company="SIX Networks GmbH" file="RepositoryOptionsContextMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using System.Windows;

using SN.withSIX.Core.Applications.MVVM.Attributes;
using SN.withSIX.Play.Applications.ViewModels.Games.Library;
using SN.withSIX.Play.Core.Games.Legacy.Repo;

namespace SN.withSIX.Play.Applications.ViewModels.Games
{
    public class RepositoryOptionsContextMenu : ModLibraryItemMenuBase<SixRepo>
    {
        public RepositoryOptionsContextMenu(ModLibraryViewModel library) : base(library) {
            Contract.Requires<ArgumentNullException>(library != null);
        }

        [MenuItem]
        public Task RemoveRepository(ContentLibraryItemViewModel<SixRepo> content) => content.Remove();

        [MenuItem]
        public void CopyRepositoryLinkToClipboard(ContentLibraryItemViewModel<SixRepo> repoItem) {
            Clipboard.SetText(repoItem.Model.GetUrl("config"));
        }
    }
}