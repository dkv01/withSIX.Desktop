// <copyright company="SIX Networks GmbH" file="BasicExternalExceptionhandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Core.Applications.Errors;

namespace withSIX.Mini.Applications
{
    public abstract class BasicExternalExceptionhandler : IHandleExceptionPlugin
    {
        public abstract UserErrorModel HandleException(Exception ex, string action = "Action");

        protected static UserErrorModel Handle(Exception exception, string action) => null;
    }
}