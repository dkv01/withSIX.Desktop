// <copyright company="SIX Networks GmbH" file="IProgress.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Helpers;

namespace withSIX.Sync.Core.Transfer
{
    public interface IProgress : ITProgress
    {
        RepoStatus Action { get; set; }
    }
}