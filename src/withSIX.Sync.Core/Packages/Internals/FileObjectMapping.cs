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
            Contract.Requires<ArgumentNullException>(filePath != null);
            Contract.Requires<ArgumentNullException>(checksum != null);
            Contract.Requires<ArgumentOutOfRangeException>(!string.IsNullOrWhiteSpace(filePath));
            Contract.Requires<ArgumentOutOfRangeException>(!string.IsNullOrWhiteSpace(checksum));
            FilePath = filePath;
            Checksum = checksum;
        }

        public string Checksum { get; protected set; }
        public string FilePath { get; protected set; }
    }
}