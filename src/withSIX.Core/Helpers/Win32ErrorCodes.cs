// <copyright company="SIX Networks GmbH" file="Win32ErrorCodes.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Core.Helpers
{
    /// <summary>
    ///     Contains relevant error codes used through our codes
    /// </summary>
    public static class Win32ErrorCodes
    {
        public const int ERROR_OUT_OF_DISKSPACE = 112;
        public const int FILE_NOT_FOUND = 2;
        public const int ACCESS_DENIED = 5;
        public const int ERROR_CANCELLED_ELEVATION = 1223;
    }
}