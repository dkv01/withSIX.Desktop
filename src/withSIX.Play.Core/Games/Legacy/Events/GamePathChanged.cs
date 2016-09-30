// <copyright company="SIX Networks GmbH" file="GamePathChanged.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using NDepend.Path;

namespace withSIX.Play.Core.Games.Legacy.Events
{
    public class GamePathChanged
    {
        public IAbsoluteDirectoryPath Path { get; }

        public GamePathChanged(IAbsoluteDirectoryPath value) {
            Path = value;
        }
    }
}