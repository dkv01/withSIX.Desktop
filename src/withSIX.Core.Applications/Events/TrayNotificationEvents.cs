// <copyright company="SIX Networks GmbH" file="TrayNotificationEvents.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Windows.Input;

namespace withSIX.Core.Applications.Events
{
    public interface ITrayNotification
    {
        string Message { get; }
        string Title { get; }
    }

    public class TrayNotification : EventArgs, ITrayNotification
    {
        public TrayNotification(string title, string message) {
            Title = title;

            Message = message;
        }

        public string Message { get; }
        public string Title { get; }
    }

    public class TwoChoiceTrayNotification : EventArgs, ITrayNotification
    {
        public TwoChoiceTrayNotification(string title, string message, ICommand yesCommand, ICommand noCommand) {
            Title = title;
            Message = message;
            YesCommand = yesCommand;
            NoCommand = noCommand;
        }

        public ICommand NoCommand { get; }
        public ICommand YesCommand { get; }

        public string Message { get; }
        public string Title { get; }
    }

    public class ThreeChoiceTrayNotification : EventArgs, ITrayNotification
    {
        public ThreeChoiceTrayNotification(string title, string message, ICommand acceptCommand,
            ICommand declineCommand,
            ICommand ignoreCommand) {
            Title = title;
            Message = message;
            AcceptCommand = acceptCommand;
            DeclineCommand = declineCommand;
            IgnoreCommand = ignoreCommand;
        }

        public ICommand AcceptCommand { get; }
        public ICommand DeclineCommand { get; }
        public ICommand IgnoreCommand { get; }

        public string Message { get; }
        public string Title { get; }
    }
}