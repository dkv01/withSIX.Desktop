// <copyright company="SIX Networks GmbH" file="IExceptionHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;

namespace SN.withSIX.Core.Applications.Errors
{
    public interface IExceptionHandlerHandle
    {
        UserErrorModel HandleException(Exception ex, string action = "Action");
    }

    public interface IExceptionHandler : IExceptionHandlerHandle
    {
        Task<bool> TryExecuteAction(Func<Task> action, string message = null);
    }

    public interface IHandleExceptionPlugin : IExceptionHandlerHandle {}
}