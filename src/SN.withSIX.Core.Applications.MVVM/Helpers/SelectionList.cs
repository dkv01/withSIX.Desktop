// <copyright company="SIX Networks GmbH" file="SelectionList.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Core.Applications.MVVM.Helpers
{
    [DataContract]
    public class SelectionList<T> : HaveReactiveItems<T>, ISelectionList<T> where T : class
    {
        T _selectedItem;
        public T SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }
    }
}