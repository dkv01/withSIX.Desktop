// <copyright company="SIX Networks GmbH" file="SixDialogExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Applications.Services;

namespace withSIX.Core.Applications.Extensions
{
    public static class SixDialogExtensions
    {
        public static bool IsYes(this SixMessageBoxResult result)
            => (result == SixMessageBoxResult.Yes) || (result == SixMessageBoxResult.YesRemember);

        public static bool IsNo(this SixMessageBoxResult result)
            => (result == SixMessageBoxResult.No) || (result == SixMessageBoxResult.NoRemember);
    }
}