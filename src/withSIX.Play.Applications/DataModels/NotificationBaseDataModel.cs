// <copyright company="SIX Networks GmbH" file="NotificationBaseDataModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Play.Applications.DataModels
{
    public abstract class NotificationBaseDataModel : DataModel
    {
        protected NotificationBaseDataModel(string categoryIcon) {
            TimeStamp = DateTime.Now;
            CategoryIcon = categoryIcon;
            OneTimeAction = true;
        }

        public DateTime TimeStamp { get; }
        public string CategoryIcon { get; }
        public IDispatchCommand OnClickDispatch { get; protected set; }
        public IDispatchCommand CloseCommand { get; private set; }
        public bool OneTimeAction { get; set; }
    }
}