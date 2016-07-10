// <copyright company="SIX Networks GmbH" file="PathDoesntExistException.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace SN.withSIX.Core
{
    public class PathDoesntExistException : Exception
    {
        public readonly string Path;
        public readonly string PathName;

        public PathDoesntExistException(string path) {
            Path = path;
        }

        public PathDoesntExistException(string path, string pathName) {
            Path = path;
            PathName = pathName;
        }
    }
}