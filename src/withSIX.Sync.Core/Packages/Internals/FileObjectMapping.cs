// <copyright company="SIX Networks GmbH" file="FileObjectMapping.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;

namespace withSIX.Sync.Core.Packages.Internals
{
    public class FileObjectMapping
    {
        public FileObjectMapping(string filePath, string checksum) {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (checksum == null) throw new ArgumentNullException(nameof(checksum));
            if (!(!string.IsNullOrWhiteSpace(filePath))) throw new ArgumentOutOfRangeException("!string.IsNullOrWhiteSpace(filePath)");
            if (!(!string.IsNullOrWhiteSpace(checksum))) throw new ArgumentOutOfRangeException("!string.IsNullOrWhiteSpace(checksum)");
            FilePath = filePath;
            Checksum = checksum;
        }

        public string Checksum { get; protected set; }
        public string FilePath { get; protected set; }
    }
}