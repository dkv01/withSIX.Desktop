// <copyright company="SIX Networks GmbH" file="ContentFolderLink.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using NDepend.Path;

namespace withSIX.Mini.Core.Games
{
    public class ContentInfo
    {
        public ContentInfo(Guid userId, Guid gameId, Guid contentId) {
            if (!(userId != Guid.Empty)) throw new ArgumentOutOfRangeException("userId != Guid.Empty");
            if (!(gameId != Guid.Empty)) throw new ArgumentOutOfRangeException("gameId != Guid.Empty");
            if (!(contentId != Guid.Empty)) throw new ArgumentOutOfRangeException("contentId != Guid.Empty");
            UserId = userId;
            GameId = gameId;
            ContentId = contentId;
        }

        public Guid UserId { get; }
        public Guid GameId { get; }
        public Guid ContentId { get; }
    }

    public class FolderInfo
    {
        public FolderInfo(IAbsoluteDirectoryPath path, ContentInfo contentInfo) {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (contentInfo == null) throw new ArgumentNullException(nameof(contentInfo));
            Path = path;
            ContentInfo = contentInfo;
        }

        public IAbsoluteDirectoryPath Path { get; }
        public ContentInfo ContentInfo { get; set; }
    }

    public class ContentFolderLink
    {
        public ContentFolderLink(List<FolderInfo> info) {
            if (info == null) throw new ArgumentNullException(nameof(info));
            Infos = info;
        }

        public List<FolderInfo> Infos { get; }
    }
}