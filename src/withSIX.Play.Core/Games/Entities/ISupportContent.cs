// <copyright company="SIX Networks GmbH" file="ISupportContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Play.Core.Games.Entities
{
    public interface ISupportContent : IHaveInstalledState
    {
        ContentPaths PrimaryContentPath { get; }
    }
}