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

        public AssemblyLoader(Assembly assembly, IAbsoluteFilePath locationOverride = null,
            IAbsoluteDirectoryPath netEntryPath = null) {
            if (assembly == null)
                throw new Exception("Entry Assembly is null!");
            _entryAssembly = assembly;
            var asName = _entryAssembly.GetName().Name;
            var netEntryFilePath = GetNetEntryFilePath(netEntryPath, asName);
            _netEntryPath = netEntryFilePath.ParentDirectoryPath;
            _entryLocation = locationOverride ?? netEntryFilePath;
            _entryPath = _entryLocation.ParentDirectoryPath;
            _entryVersion = _entryAssembly.GetName().Version;
            _entryAssemblyName = asName;
        }

        private static IAbsoluteFilePath GetNetEntryFilePath(IAbsoluteDirectoryPath netEntryPath, string asName) {
            var en = netEntryPath ?? AppContext.BaseDirectory.ToAbsoluteDirectoryPath();
            var dll = en.GetChildFileWithName(asName + ".dll");
            var netEntryFilePath = dll.Exists ? dll : en.GetChildFileWithName(asName + ".exe");
                //_entryAssembly.Location.ToAbsoluteFilePath();
            return netEntryFilePath;
        }

        #region IAssemblyLoader Members

        public Version GetEntryVersion() => _entryVersion;

        public string GetProductVersion() {
            var attr = _entryAssembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))
                as AssemblyInformationalVersionAttribute;
            return attr.InformationalVersion;
        }

        public string GetEntryAssemblyName() => _entryAssemblyName;

        public IAbsoluteDirectoryPath GetNetEntryPath() => _netEntryPath;

        public IAbsoluteDirectoryPath GetEntryPath() => _entryPath;

        public IAbsoluteFilePath GetEntryLocation() => _entryLocation;

        public string GetInformationalVersion()
            => FileVersionInfo.GetVersionInfo(_entryLocation.ToString())?.ProductVersion;

        #endregion
    }
}