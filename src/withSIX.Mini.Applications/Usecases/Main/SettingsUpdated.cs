// <copyright company="SIX Networks GmbH" file="SettingsUpdated.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core;

namespace withSIX.Mini.Applications.Usecases.Main
{
    public class SettingsUpdated : AsyncDomainEvent<Models.Settings>
    {
        public SettingsUpdated(Models.Settings subject) : base(subject) {}
    }
}