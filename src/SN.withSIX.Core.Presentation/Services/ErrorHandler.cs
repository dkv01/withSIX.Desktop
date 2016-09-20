// <copyright company="SIX Networks GmbH" file="ErrorHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using ReactiveUI;
using SN.withSIX.Core.Applications.Errors;

namespace SN.withSIX.Core.Presentation.Services
{
    public abstract class ErrorHandler
    {
        // We cant use SmartAss in non merged assemblies..
        public static Action<Exception> Report { get; set; } = x => { };
        public static IStdErrorHandler Handler { get; set; }
    }

    public interface IStdErrorHandler
    {
        Task<RecoveryOptionResult> Handler(UserError error);
    }
}