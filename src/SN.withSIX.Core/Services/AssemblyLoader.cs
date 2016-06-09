// <copyright company="SIX Networks GmbH" file="AssemblyLoader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Reflection;
using NDepend.Path;

namespace SN.withSIX.Core.Services
{
    public class AssemblyLoader : IAssemblyLoader, IDomainService
    {
        readonly Assembly _entryAssembly;
        readonly string _entryAssemblyName;

        readonly IAbsoluteFilePath _entryLocation;
        readonly IAbsoluteDirectoryPath _entryPath;
        readonly Version _entryVersion;
        readonly IAbsoluteDirectoryPath _netEntryPath;

        public AssemblyLoader(Assembly assembly, IAbsoluteFilePath locationOverride = null) {
            if (assembly == null)
                throw new Exception("Entry Assembly is null!");
            _entryAssembly = assembly;
            var netEntryFilePath = _entryAssembly.Location.ToAbsoluteFilePath();
            _netEntryPath = netEntryFilePath.ParentDirectoryPath;
            _entryLocation = locationOverride ?? netEntryFilePath;
            _entryPath = _entryLocation.ParentDirectoryPath;
            _entryVersion = _entryAssembly.GetName().Version;
            _entryAssemblyName = _entryAssembly.GetName().Name;
        }

        #region IAssemblyLoader Members

        public Version GetEntryVersion() => _entryVersion;

        public string GetProductVersion() {
            var attr = Attribute
                .GetCustomAttribute(
                    _entryAssembly,
                    typeof (AssemblyInformationalVersionAttribute))
                as AssemblyInformationalVersionAttribute;
            return attr.InformationalVersion;
        }


        public string GetEntryAssemblyName() => _entryAssemblyName;

        public IAbsoluteDirectoryPath GetNetEntryPath() => _netEntryPath;

        public IAbsoluteDirectoryPath GetEntryPath() => _entryPath;

        public IAbsoluteFilePath GetEntryLocation() => _entryLocation;

        public string GetInformationalVersion() => _entryAssembly.GetInformationalVersion();

        #endregion
    }

    public static class AssemblyExtensions
    {
        public static string GetInformationalVersion(this Assembly assembly)
            => FileVersionInfo.GetVersionInfo(assembly.Location)?.ProductVersion;
    }
}