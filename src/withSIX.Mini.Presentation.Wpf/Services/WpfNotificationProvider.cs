﻿// <copyright company="SIX Networks GmbH" file="WpfNotificationProvider.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications;
using withSIX.Mini.Applications.MVVM.ViewModels;

namespace withSIX.Mini.Presentation.Wpf.Services
{
    public class WpfNotificationProvider : INotificationProvider
    {
        public Task<bool?> Notify(string subject, string text, string icon = null, TimeSpan? expirationTime = null,
            params TrayAction[] actions) {
            // TODO: How else to communicate with the MainWindow that hosts the TaskbarIcon??
            new ShowTrayNotification(subject, text, icon, expirationTime, actions).PublishToMessageBus();
            return Task.FromResult((bool?) false);
        }
    }
}