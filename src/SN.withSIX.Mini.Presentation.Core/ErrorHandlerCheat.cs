// <copyright company="SIX Networks GmbH" file="ErrorHandlerCheat.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using ReactiveUI;

namespace SN.withSIX.Mini.Presentation.Core
{
    // These are here, in non .netstandard land because of ReactiveUI references, code contract issues.
    // We need RXUI7!
    public static class ErrorHandlerCheat
    {
        public static IStdErrorHandler Handler { get; set; }
    }

    public interface IStdErrorHandler
    {
        Task<RecoveryOptionResult> Handler(UserError error);
    }
}