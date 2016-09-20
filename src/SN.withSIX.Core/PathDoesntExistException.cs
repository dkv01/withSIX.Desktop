// <copyright company="SIX Networks GmbH" file="PathDoesntExistException.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace SN.withSIX.Core
{
    public class PathDoesntExistException : Exception
    {
        public string Path { get; }
        public string PathName { get; }

        public PathDoesntExistException(string path) {
            Path = path;
        }

        public PathDoesntExistException(string path, string pathName) {
            Path = path;
            PathName = pathName;
        }
    }


    public abstract class SetupException : InvalidOperationException
    {
        protected SetupException(string message) : base(message) { }
        protected SetupException(string message, Exception ex) : base(message, ex) { }
    }

    public class DidNotStartException : SetupException
    {
        public DidNotStartException(string message) : base(message) { }
        public DidNotStartException(string message, Exception ex) : base(message, ex) { }
    }
}