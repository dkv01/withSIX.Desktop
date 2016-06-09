// <copyright company="SIX Networks GmbH" file="INotificationProvider.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using ReactiveUI;
using ShortBus;

namespace SN.withSIX.Core.Applications.Services
{
    public interface INotificationProvider
    {
        Task<bool?> Notify(string subject, string text, string icon = null, TimeSpan? expirationTime = null,
            params TrayAction[] actions);
    }

    public class TrayAction
    {
        public string DisplayName { get; set; }
        public IReactiveCommand<UnitType> Command { get; set; }
    }
}