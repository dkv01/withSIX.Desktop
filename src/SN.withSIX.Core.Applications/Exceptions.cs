// <copyright company="SIX Networks GmbH" file="Exceptions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using withSIX.Api.Models.Exceptions;

namespace SN.withSIX.Core.Applications
{
    [Serializable]
    public class NotEnoughFreeDiskSpaceException : UserException
    {
        public NotEnoughFreeDiskSpaceException(string message, Exception innerException) : base(message, innerException) {}
        public NotEnoughFreeDiskSpaceException(string message) : base(message) {}

        //[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)] protected NotEnoughFreeDiskSpaceException(SerializationInfo info, StreamingContext context) //  : base(info, context) {}
    }

    [Serializable]
    public class CanceledException : UserException
    {
        private const string TheUserCancelledTheOperation = "The user cancelled the operation";

        [Obsolete("Unless needed please use at least create this exception with a useful message")]
        public CanceledException() : base(TheUserCancelledTheOperation) {}

        public CanceledException(string message) : base(message) {}
        public CanceledException(string message, Exception inner) : base(message, inner) {}
        public CanceledException(Exception inner) : base(TheUserCancelledTheOperation, inner) {}

        //[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        //protected CanceledException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}