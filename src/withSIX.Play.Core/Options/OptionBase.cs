// <copyright company="SIX Networks GmbH" file="OptionBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Core.Helpers;

namespace withSIX.Play.Core.Options
{
    [DataContract]
    public abstract class OptionBase : PropertyChangedBase
    {
        protected static void SaveSettings() {
            DomainEvilGlobal.Settings.RaiseChanged();
        }
    }
}