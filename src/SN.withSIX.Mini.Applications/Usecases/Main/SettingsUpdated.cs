// <copyright company="SIX Networks GmbH" file="SettingsUpdated.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Core;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public class SettingsUpdated : IDomainEvent
    {
        public SettingsUpdated(Models.Settings settings) {
            Settings = settings;
        }

        public Models.Settings Settings { get; }
    }
}