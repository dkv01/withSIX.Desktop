// <copyright company="SIX Networks GmbH" file="IAddonSigner.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using NDepend.Path;

namespace withSIX.Sync.Core.ExternalTools
{
    public interface IAddonSigner
    {
        void SignFile(IAbsoluteFilePath file, IAbsoluteFilePath privateFile, bool repackOnFailure = false);
        void SignFolder(IAbsoluteDirectoryPath folder, IAbsoluteFilePath privateFile, bool repackOnFailure = false);
        void SignAllInFolder(IAbsoluteDirectoryPath folder, IAbsoluteFilePath privateFile, bool repackOnFailure = false);
        void SignMany(AddonSigner.SignManyParams spec);
    }
}