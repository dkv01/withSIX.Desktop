// <copyright company="SIX Networks GmbH" file="WriteParFileInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;

namespace withSIX.Mini.Plugin.Arma.Services
{
    public class WriteParFileInfo
    {
        public WriteParFileInfo(Guid gameId, string content) {
            if (!(gameId != Guid.Empty)) throw new ArgumentNullException("gameId != Guid.Empty");
            if (content == null) throw new ArgumentNullException(nameof(content));
            GameId = gameId;
            Content = content;
        }

        public WriteParFileInfo(Guid gameId, string content, string additionalIdentifier)
            : this(gameId, content) {
            if (!(!string.IsNullOrWhiteSpace(additionalIdentifier))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(additionalIdentifier)");
            AdditionalIdentifier = additionalIdentifier;
        }

        public string Content { get; }
        public string AdditionalIdentifier { get; }
        public Guid GameId { get; }
    }
}