// <copyright company="SIX Networks GmbH" file="ArchiveFormatUnsupported.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace SN.withSIX.Sync.Core.Legacy.SixSync
{
    public class ArchiveFormatUnsupported : Exception
    {
        public ArchiveFormatUnsupported() {}
        public ArchiveFormatUnsupported(string message) : base(message) {}
    }
}