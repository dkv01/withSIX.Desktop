// <copyright company="SIX Networks GmbH" file="UpdateAvailableDataModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Applications;

namespace withSIX.Play.Applications.DataModels.Notifications
{
    public class UpdateAvailableDataModel : NotificationBaseDataModel
    {
        public UpdateAvailableDataModel(string version, string icon)
            : base(SixIconFont.withSIX_icon_Download) {
            Version = version;
            Icon = icon;
        }

        public string Version { get; }
        public string Icon { get; }
    }
}