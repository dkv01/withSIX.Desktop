// <copyright company="SIX Networks GmbH" file="UnhandledExceptionHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Presentation;
using withSIX.Core.Presentation.Services;

namespace withSIX.Mini.Presentation.Core
{
    public class UnhandledExceptionHandler : DefaultExceptionHandler, IPresentationService
    {
        public UnhandledExceptionHandler(IEnumerable<IHandleExceptionPlugin> ehs) : base(ehs) {}

        protected override UserErrorModel HandleExceptionInternal(Exception ex, string action = "Action") {
            Contract.Requires<ArgumentNullException>(action != null);
            return Handle((dynamic) ex, action);
        }
    }
}