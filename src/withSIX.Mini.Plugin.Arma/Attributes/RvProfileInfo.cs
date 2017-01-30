// <copyright company="SIX Networks GmbH" file="RvProfileInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Mini.Plugin.Arma.Attributes
{
    public class RvProfileInfoAttribute : Attribute
    {
        public RvProfileInfoAttribute(string mainName, string otherProfilesName, string profileExtension) {
            //            if (!(!string.IsNullOrWhiteSpace(mainName))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(mainName)");
            //            if (!(!string.IsNullOrWhiteSpace(otherProfilesName))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(otherProfilesName)");
            //            if (!(!string.IsNullOrWhiteSpace(profileExtension))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(profileExtension)");

            DocumentsMainName = mainName;
            DocumentsOtherProfilesName = otherProfilesName;
            ProfileExtension = profileExtension;
        }

        public string DocumentsMainName { get; }
        public string DocumentsOtherProfilesName { get; }
        public string ProfileExtension { get; }
    }
}