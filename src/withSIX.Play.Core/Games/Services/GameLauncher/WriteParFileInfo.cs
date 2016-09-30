// <copyright company="SIX Networks GmbH" file="WriteParFileInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;

namespace withSIX.Play.Core.Games.Services.GameLauncher
{
    public class WriteParFileInfo
    {
        public WriteParFileInfo(Guid gameId, string content) {
            Contract.Requires<ArgumentNullException>(gameId != Guid.Empty);
            Contract.Requires<ArgumentNullException>(content != null);
            GameId = gameId;
            Content = content;
        }

        public WriteParFileInfo(Guid gameId, string content, string additionalIdentifier)
            : this(gameId, content) {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(additionalIdentifier));
            AdditionalIdentifier = additionalIdentifier;
        }

        public string Content { get; }
        public string AdditionalIdentifier { get; }
        public Guid GameId { get; }
    }
}