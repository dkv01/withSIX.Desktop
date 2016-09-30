// <copyright company="SIX Networks GmbH" file="UploadCollectionPopupMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;

using withSIX.Api.Models.Collections;
using withSIX.Core.Applications.MVVM.Attributes;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Play.Applications.ViewModels.Games.Library;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public class UploadCollectionPopupMenu : PopupMenuBase<CustomCollectionLibraryItemViewModel>
    {
        readonly UploadCollection _uploadCollection;

        public UploadCollectionPopupMenu(UploadCollection uploadCollection) {
            _uploadCollection = uploadCollection;
            Header = "Upload Collection";
        }

        [MenuItem]
        public Task Private(CustomCollectionLibraryItemViewModel item) => _uploadCollection.Publish(item, CollectionScope.Private);

        [MenuItem]
        public Task Unlisted(CustomCollectionLibraryItemViewModel item) => _uploadCollection.Publish(item, CollectionScope.Unlisted);

        [MenuItem]
        public Task Public(CustomCollectionLibraryItemViewModel item) => _uploadCollection.Publish(item, CollectionScope.Public);
    }
}