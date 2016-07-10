// <copyright company="SIX Networks GmbH" file="AbortedException.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;


namespace SN.withSIX.Sync.Core.Legacy
{
    
    public class AbortedException : Exception
    {
        public AbortedException() {}
        public AbortedException(string message) : base(message) {}
    }
}