// <copyright company="SIX Networks GmbH" file="FriendServerChangedDataModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Applications;
using withSIX.Core.Applications.Extensions;
using withSIX.Play.Applications.UseCases;

namespace withSIX.Play.Applications.DataModels.Notifications
{
    public class FriendServerChangedDataModel : NotificationBaseDataModel
    {
        public FriendServerChangedDataModel(string accountName, string serverName)
            : base(SixIconFont.withSIX_icon_Cloud) {
            AccountName = accountName;
            ServerName = serverName;
            OnClickDispatch = new DispatchCommand<FriendServerChangedCommand>(new FriendServerChangedCommand());
        }

        public string AccountName { get; }
        public string ServerName { get; }
    }
}