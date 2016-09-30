// <copyright company="SIX Networks GmbH" file="CollectionVisibilityViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI.Legacy;
using withSIX.Api.Models.Collections;

namespace withSIX.Play.Applications.ViewModels.Games.Dialogs
{
    public interface ICollectionVisibilityViewModel {}

    
    public class CollectionVisibilityViewModel : DialogBase, ICollectionVisibilityViewModel
    {
        CollectionScope _visibility = CollectionScope.Private;

        public CollectionVisibilityViewModel() {
            DisplayName = "Upload your Collection";
            this.SetCommand(x => x.OkCommand).Subscribe(() => TryClose(true));
            this.SetCommand(x => x.CancelCommand).Subscribe(() => TryClose(false));
        }

        public CollectionScope Visibility
        {
            get { return _visibility; }
            set { SetProperty(ref _visibility, value); }
        }
        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }
    }
}