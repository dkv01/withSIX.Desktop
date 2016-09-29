// <copyright company="SIX Networks GmbH" file="BiKeyPair.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using NDepend.Path;
using withSIX.Core.Helpers;

namespace withSIX.Sync.Core.ExternalTools
{
    public class BiKeyPair : PropertyChangedBase
    {
        bool _isSelected;

        public BiKeyPair(IAbsoluteFilePath path) {
            Contract.Requires<ArgumentNullException>(path != null);
            Contract.Requires<ArgumentOutOfRangeException>(!path.FileName.Contains("@"));

            // TODO: Make this better!
            var p = path.ParentDirectoryPath.GetChildFileWithName(path.FileNameWithoutExtension).ToString();
            Location = path.ParentDirectoryPath.ToString();
            Name = path.FileNameWithoutExtension;
            CreatedAt = path.Exists ? path.FileInfo.CreationTimeUtc : DateTime.UtcNow;
            PrivateFile = (p + ".biprivatekey").ToAbsoluteFilePath();
            PublicFile = (p + ".bikey").ToAbsoluteFilePath();
        }

        public IAbsoluteFilePath PrivateFile { get; }
        public IAbsoluteFilePath PublicFile { get; }
        public DateTime CreatedAt { get; set; }
        public string Name { get; protected set; }
        public string Location { get; protected set; }

        public static BiKeyPair CreateSignKey(IAbsoluteFilePath path, PboTools pboTools) {
            CreateKey(path, pboTools);
            return new BiKeyPair(path);
        }

        public static void CreateKey(IAbsoluteFilePath path, PboTools pboTools) {
            Contract.Requires<ArgumentOutOfRangeException>(!path.FileName.Contains("@"));
            Contract.Requires<ArgumentOutOfRangeException>(!path.FileName.Contains(".biprivatekey"));
            Contract.Requires<ArgumentOutOfRangeException>(!path.FileName.Contains(".bikey"));
            pboTools.CreateKey(path);
        }
    }
}