// <copyright company="SIX Networks GmbH" file="ChecksumException.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;


namespace SN.withSIX.Sync.Core.Repositories.Internals
{
    
    public class ChecksumException : Exception
    {
        public ChecksumException() {}
        public ChecksumException(string message) : base(message) {}
        public ChecksumException(string message, Exception innerException) : base(message, innerException) {}
    }
}