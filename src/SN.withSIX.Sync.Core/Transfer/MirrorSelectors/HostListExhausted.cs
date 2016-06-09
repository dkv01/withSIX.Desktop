// <copyright company="SIX Networks GmbH" file="HostListExhausted.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using SmartAssembly.Attributes;

namespace SN.withSIX.Sync.Core.Transfer.MirrorSelectors
{
    [DoNotObfuscate]
    public class HostListExhausted : TransferException
    {
        public HostListExhausted() : this("The host list was exhausted") {}
        public HostListExhausted(string message) : base(message) {}
        public HostListExhausted(string message, Exception inner) : base(message, inner) {}
    }

    [DoNotObfuscate]
    public class TooManyProgramExceptions : TransferException
    {
        public TooManyProgramExceptions() : this("Too many external program errors occurred") {}
        public TooManyProgramExceptions(string message) : base(message) {}
        public TooManyProgramExceptions(string message, Exception inner) : base(message, inner) {}
    }
}