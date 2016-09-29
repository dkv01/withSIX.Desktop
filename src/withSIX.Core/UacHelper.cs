// <copyright company="SIX Networks GmbH" file="UacHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;

namespace withSIX.Core
{
    public interface IUacHelper
    {
        bool IsUacEnabled();
        bool IsProcessElevated();
        bool CheckUac();
        IEnumerable<string> GetStartupParameters();
    }
}