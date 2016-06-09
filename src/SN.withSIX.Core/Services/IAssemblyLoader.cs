// <copyright company="SIX Networks GmbH" file="IAssemblyLoader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using NDepend.Path;

namespace SN.withSIX.Core.Services
{
    public interface IAssemblyInfo
    {
        string GetProductVersion();
        Version GetEntryVersion();
        string GetEntryAssemblyName();
        IAbsoluteDirectoryPath GetEntryPath();
        IAbsoluteFilePath GetEntryLocation();
        IAbsoluteDirectoryPath GetNetEntryPath();
        string GetInformationalVersion();
    }

    public interface IAssemblyLoader : IAssemblyInfo {}
}