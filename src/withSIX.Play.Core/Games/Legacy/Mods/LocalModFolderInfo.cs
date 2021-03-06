// <copyright company="SIX Networks GmbH" file="LocalModFolderInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using NDepend.Path;

namespace withSIX.Play.Core.Games.Legacy.Mods
{
    public class LocalModFolderInfo : LocalModInfo
    {
        public LocalModFolderInfo(IAbsoluteDirectoryPath path) {
            if (path == null) throw new ArgumentNullException(nameof(path));
            Name = path.DirectoryName;
            Path = path.ParentDirectoryPath;
        }

        public override string Name { get; protected set; }
        public override IAbsoluteDirectoryPath Path { get; protected set; }
    }
}