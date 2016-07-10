// <copyright company="SIX Networks GmbH" file="Rest.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;


namespace SN.withSIX.Core.Applications.Infrastructure
{
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    
    public abstract class RestExceptionBase : Exception
    {
        protected RestExceptionBase(string message)
            : base(message) {}

        protected RestExceptionBase(string message, Exception innerException)
            : base(message, innerException) {}
    }

    
    public class RestResponseException : RestExceptionBase
    {
        public RestResponseException(string message)
            : base(message) {}

        public RestResponseException(string message, Exception innerException)
            : base(message, innerException) {}
    }

    
    public class RestStatusException : RestExceptionBase
    {
        public RestStatusException(string message)
            : base(message) {}

        public RestStatusException(string message, Exception innerException)
            : base(message, innerException) {}
    }
}