// <copyright company="SIX Networks GmbH" file="ISendMessage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Play.Core.Games.Legacy.Arma.Commands
{
    public interface ISendMessage : IMessage
    {
        string ToGameCommand();
    }
}