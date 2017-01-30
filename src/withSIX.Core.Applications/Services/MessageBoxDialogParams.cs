// <copyright company="SIX Networks GmbH" file="MessageBoxDialogParams.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Core.Applications.Services
{
    public class MessageBoxDialogParams
    {
        public MessageBoxDialogParams(string message) {
            // if (message == null) throw new ArgumentNullException(nameof(message));
            Message = message;
            IgnoreContent = true;
        }

        public MessageBoxDialogParams(string message, string title)
            : this(message) {
            // if (title == null) throw new ArgumentNullException(nameof(title));
            Title = title;
        }

        public MessageBoxDialogParams(string message, string title, SixMessageBoxButton buttons)
            : this(message, title) {
            Buttons = buttons;
        }

        public string Message { get; }
        public string Title { get; }
        public SixMessageBoxButton Buttons { get; }
        public string GreenContent { get; set; }
        public string BlueContent { get; set; }
        public string RedContent { get; set; }
        public bool? RememberedState { get; set; }
        public object Owner { get; set; }
        public bool IgnoreContent { get; set; }
    }
}