// <copyright company="SIX Networks GmbH" file="UnhandledUserException.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Mini.Applications.Features
{
    public class UnhandledUserException : Exception
    {
        public UnhandledUserException(string s, Exception exception) : base(s, exception) {}
    }
}