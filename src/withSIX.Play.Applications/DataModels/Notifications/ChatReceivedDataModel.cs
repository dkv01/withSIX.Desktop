// <copyright company="SIX Networks GmbH" file="ChatReceivedDataModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Play.Applications.UseCases;

namespace withSIX.Play.Applications.DataModels.Notifications
{
    public class ChatReceivedDataModel : NotificationBaseDataModel
    {
        public ChatReceivedDataModel(string fromUserName, Uri fromAvatar, string message)
            : base(SixIconFont.withSIX_icon_Chat_Message) {
            FromUserName = fromUserName;
            FromAvatar = fromAvatar;
            Message = message;
            OnClickDispatch = new DispatchCommand<ChatMessageRecievedCommand>(new ChatMessageRecievedCommand());
        }

        public string FromUserName { get; }
        public Uri FromAvatar { get; }
        public string Message { get; }
    }
}