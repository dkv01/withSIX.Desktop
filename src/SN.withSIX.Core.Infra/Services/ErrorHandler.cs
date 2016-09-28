// <copyright company="SIX Networks GmbH" file="ErrorHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace SN.withSIX.Core.Infra.Services
{
    public abstract class ErrorHandler
    {
        // We cant use SmartAss in non merged assemblies..
        public static Action<Exception> Report { get; set; } = x => { };
    }
}