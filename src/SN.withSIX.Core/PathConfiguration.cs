﻿// <copyright company="SIX Networks GmbH" file="PathConfiguration.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using NDepend.Path;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services;

namespace SN.withSIX.Core
{
    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    // See the LICENSE file in the project root for more information.

    public static partial class EnvironmentSpecial
    {
        public enum SpecialFolderOption
        {
            None = 0,
            Create = SpecialFolderOptionValues.CSIDL_FLAG_CREATE,
            DoNotVerify = SpecialFolderOptionValues.CSIDL_FLAG_DONT_VERIFY
        }

        // These values are specific to Windows and are known to SHGetFolderPath, however they are
        // also the values used in the SpecialFolderOption enum.  As such, we keep them as constants
        // with their Win32 names, but keep them here rather than in Interop.mincore as they're
        // used on all platforms.
        private static class SpecialFolderOptionValues
        {
            internal const int CSIDL_FLAG_CREATE = 0x8000; // force folder creation in SHGetFolderPath
            internal const int CSIDL_FLAG_DONT_VERIFY = 0x4000; // return an unverified folder path
        }
    }

    public static partial class EnvironmentSpecial
    {
        public enum SpecialFolder
        {
            ApplicationData = SpecialFolderValues.CSIDL_APPDATA,
            CommonApplicationData = SpecialFolderValues.CSIDL_COMMON_APPDATA,
            LocalApplicationData = SpecialFolderValues.CSIDL_LOCAL_APPDATA,
            Cookies = SpecialFolderValues.CSIDL_COOKIES,
            Desktop = SpecialFolderValues.CSIDL_DESKTOP,
            Favorites = SpecialFolderValues.CSIDL_FAVORITES,
            History = SpecialFolderValues.CSIDL_HISTORY,
            InternetCache = SpecialFolderValues.CSIDL_INTERNET_CACHE,
            Programs = SpecialFolderValues.CSIDL_PROGRAMS,
            MyComputer = SpecialFolderValues.CSIDL_DRIVES,
            MyMusic = SpecialFolderValues.CSIDL_MYMUSIC,
            MyPictures = SpecialFolderValues.CSIDL_MYPICTURES,
            MyVideos = SpecialFolderValues.CSIDL_MYVIDEO,
            Recent = SpecialFolderValues.CSIDL_RECENT,
            SendTo = SpecialFolderValues.CSIDL_SENDTO,
            StartMenu = SpecialFolderValues.CSIDL_STARTMENU,
            Startup = SpecialFolderValues.CSIDL_STARTUP,
            System = SpecialFolderValues.CSIDL_SYSTEM,
            Templates = SpecialFolderValues.CSIDL_TEMPLATES,
            DesktopDirectory = SpecialFolderValues.CSIDL_DESKTOPDIRECTORY,
            Personal = SpecialFolderValues.CSIDL_PERSONAL,
            MyDocuments = SpecialFolderValues.CSIDL_PERSONAL,
            ProgramFiles = SpecialFolderValues.CSIDL_PROGRAM_FILES,
            CommonProgramFiles = SpecialFolderValues.CSIDL_PROGRAM_FILES_COMMON,
            AdminTools = SpecialFolderValues.CSIDL_ADMINTOOLS,
            CDBurning = SpecialFolderValues.CSIDL_CDBURN_AREA,
            CommonAdminTools = SpecialFolderValues.CSIDL_COMMON_ADMINTOOLS,
            CommonDocuments = SpecialFolderValues.CSIDL_COMMON_DOCUMENTS,
            CommonMusic = SpecialFolderValues.CSIDL_COMMON_MUSIC,
            CommonOemLinks = SpecialFolderValues.CSIDL_COMMON_OEM_LINKS,
            CommonPictures = SpecialFolderValues.CSIDL_COMMON_PICTURES,
            CommonStartMenu = SpecialFolderValues.CSIDL_COMMON_STARTMENU,
            CommonPrograms = SpecialFolderValues.CSIDL_COMMON_PROGRAMS,
            CommonStartup = SpecialFolderValues.CSIDL_COMMON_STARTUP,
            CommonDesktopDirectory = SpecialFolderValues.CSIDL_COMMON_DESKTOPDIRECTORY,
            CommonTemplates = SpecialFolderValues.CSIDL_COMMON_TEMPLATES,
            CommonVideos = SpecialFolderValues.CSIDL_COMMON_VIDEO,
            Fonts = SpecialFolderValues.CSIDL_FONTS,
            NetworkShortcuts = SpecialFolderValues.CSIDL_NETHOOD,
            PrinterShortcuts = SpecialFolderValues.CSIDL_PRINTHOOD,
            UserProfile = SpecialFolderValues.CSIDL_PROFILE,
            CommonProgramFilesX86 = SpecialFolderValues.CSIDL_PROGRAM_FILES_COMMONX86,
            ProgramFilesX86 = SpecialFolderValues.CSIDL_PROGRAM_FILESX86,
            Resources = SpecialFolderValues.CSIDL_RESOURCES,
            LocalizedResources = SpecialFolderValues.CSIDL_RESOURCES_LOCALIZED,
            SystemX86 = SpecialFolderValues.CSIDL_SYSTEMX86,
            Windows = SpecialFolderValues.CSIDL_WINDOWS
        }

        // These values are specific to Windows and are known to SHGetFolderPath, however they are
        // also the values used in the SpecialFolder enum.  As such, we keep them as constants
        // with their Win32 names, but keep them here rather than in Interop.mincore as they're
        // used on all platforms.
        private static class SpecialFolderValues
        {
            internal const int CSIDL_APPDATA = 0x001a;
            internal const int CSIDL_COMMON_APPDATA = 0x0023;
            internal const int CSIDL_LOCAL_APPDATA = 0x001c;
            internal const int CSIDL_COOKIES = 0x0021;
            internal const int CSIDL_FAVORITES = 0x0006;
            internal const int CSIDL_HISTORY = 0x0022;
            internal const int CSIDL_INTERNET_CACHE = 0x0020;
            internal const int CSIDL_PROGRAMS = 0x0002;
            internal const int CSIDL_RECENT = 0x0008;
            internal const int CSIDL_SENDTO = 0x0009;
            internal const int CSIDL_STARTMENU = 0x000b;
            internal const int CSIDL_STARTUP = 0x0007;
            internal const int CSIDL_SYSTEM = 0x0025;
            internal const int CSIDL_TEMPLATES = 0x0015;
            internal const int CSIDL_DESKTOPDIRECTORY = 0x0010;
            internal const int CSIDL_PERSONAL = 0x0005;
            internal const int CSIDL_PROGRAM_FILES = 0x0026;
            internal const int CSIDL_PROGRAM_FILES_COMMON = 0x002b;
            internal const int CSIDL_DESKTOP = 0x0000;
            internal const int CSIDL_DRIVES = 0x0011;
            internal const int CSIDL_MYMUSIC = 0x000d;
            internal const int CSIDL_MYPICTURES = 0x0027;

            internal const int CSIDL_ADMINTOOLS = 0x0030; // <user name>\Start Menu\Programs\Administrative Tools
            internal const int CSIDL_CDBURN_AREA = 0x003b;
                // USERPROFILE\Local Settings\Application Data\Microsoft\CD Burning
            internal const int CSIDL_COMMON_ADMINTOOLS = 0x002f; // All Users\Start Menu\Programs\Administrative Tools
            internal const int CSIDL_COMMON_DOCUMENTS = 0x002e; // All Users\Documents
            internal const int CSIDL_COMMON_MUSIC = 0x0035; // All Users\My Music
            internal const int CSIDL_COMMON_OEM_LINKS = 0x003a; // Links to All Users OEM specific apps
            internal const int CSIDL_COMMON_PICTURES = 0x0036; // All Users\My Pictures
            internal const int CSIDL_COMMON_STARTMENU = 0x0016; // All Users\Start Menu
            internal const int CSIDL_COMMON_PROGRAMS = 0X0017; // All Users\Start Menu\Programs
            internal const int CSIDL_COMMON_STARTUP = 0x0018; // All Users\Startup
            internal const int CSIDL_COMMON_DESKTOPDIRECTORY = 0x0019; // All Users\Desktop
            internal const int CSIDL_COMMON_TEMPLATES = 0x002d; // All Users\Templates
            internal const int CSIDL_COMMON_VIDEO = 0x0037; // All Users\My Video
            internal const int CSIDL_FONTS = 0x0014; // windows\fonts
            internal const int CSIDL_MYVIDEO = 0x000e; // "My Videos" folder
            internal const int CSIDL_NETHOOD = 0x0013; // %APPDATA%\Microsoft\Windows\Network Shortcuts
            internal const int CSIDL_PRINTHOOD = 0x001b; // %APPDATA%\Microsoft\Windows\Printer Shortcuts
            internal const int CSIDL_PROFILE = 0x0028; // %USERPROFILE% (%SystemDrive%\Users\%USERNAME%)
            internal const int CSIDL_PROGRAM_FILES_COMMONX86 = 0x002c; // x86 Program Files\Common on RISC
            internal const int CSIDL_PROGRAM_FILESX86 = 0x002a; // x86 C:\Program Files on RISC
            internal const int CSIDL_RESOURCES = 0x0038; // %windir%\Resources
            internal const int CSIDL_RESOURCES_LOCALIZED = 0x0039; // %windir%\resources\0409 (code page)
            internal const int CSIDL_SYSTEMX86 = 0x0029; // %windir%\system32
            internal const int CSIDL_WINDOWS = 0x0024; // GetWindowsDirectory()
        }
    }

    public class PathConfiguration : IPathConfiguration, IDomainService, IEnableLogging
    {
        const string CompanyPath = "SIX Networks";
        public static Func<EnvironmentSpecial.SpecialFolder, string> GetFolderPath { get; set; }
        public IAbsoluteFilePath CmdExe { get; } =
            Path.Combine(GetFolderPath(EnvironmentSpecial.SpecialFolder.System), "cmd.exe").ToAbsoluteFilePath();
        public string SelfUpdaterExe { get; } = "withSIX-SelfUpdater.exe";
        public string ServiceExe { get; } = "withSIX-Updater.exe";
        public IAbsoluteDirectoryPath ProgramDataPath { get; private set; }
        public IAbsoluteDirectoryPath SharedFilesPath { get; private set; }
        public IAbsoluteFilePath ServiceExePath { get; private set; }
        public IAbsoluteFilePath SelfUpdaterExePath { get; private set; }
        public IAbsoluteDirectoryPath SharedDllPath { get; private set; }
        public IAbsoluteDirectoryPath LocalDataSharedPath { get; private set; }
        public IAbsoluteDirectoryPath EntryPath { get; private set; }
        public IAbsoluteFilePath EntryLocation { get; private set; }
        public IAbsoluteDirectoryPath AwesomiumPath { get; set; }
        public IAbsoluteDirectoryPath NotePath { get; private set; }
        public IAbsoluteDirectoryPath LocalDataRootPath { get; private set; }
        public IAbsoluteDirectoryPath SynqRootPath { get; private set; }
        public IAbsoluteDirectoryPath AppPath { get; private set; }
        public IAbsoluteDirectoryPath ConfigPath { get; private set; }
        public IAbsoluteDirectoryPath DataPath { get; private set; }
        public IAbsoluteDirectoryPath LocalDataPath { get; private set; }
        public IAbsoluteDirectoryPath LogPath { get; private set; }
        public IAbsoluteDirectoryPath MyDocumentsPath { get; private set; }
        public IAbsoluteDirectoryPath TempPath { get; private set; }
        public IAbsoluteDirectoryPath ToolMinGwBinPath { get; private set; }
        public IAbsoluteDirectoryPath ToolCygwinBinPath { get; private set; }
        public IAbsoluteDirectoryPath ToolPath { get; private set; }
        //public string ToolsPath { get; private set; }
        public bool PathsSet { get; private set; }
        public IAbsoluteDirectoryPath StartPath { get; private set; }
        // TODO

        public virtual void SetPaths(IAbsoluteDirectoryPath appPath = null, IAbsoluteDirectoryPath dataPath = null,
            IAbsoluteDirectoryPath localDataPath = null, IAbsoluteDirectoryPath tempPath = null,
            IAbsoluteDirectoryPath configPath = null, IAbsoluteDirectoryPath toolPath = null,
            IAbsoluteDirectoryPath sharedDataPath = null) {
            //if (PathsSet) throw new Exception("Paths are already set!");
            if (PathsSet)
                this.Logger().Debug("Paths were already set!");

            EntryLocation = CommonBase.AssemblyLoader.GetEntryLocation();
            EntryPath = CommonBase.AssemblyLoader.GetEntryPath();
            ProcessExtensions.DefaultWorkingDirectory = EntryPath;
            AppPath = appPath ?? GetAppPath();
            DataPath = dataPath ?? GetDataPath();
            NotePath = DataPath.GetChildDirectoryWithName("Notes");
            LocalDataRootPath = GetLocalDataRootPath();
            LocalDataPath = localDataPath ?? GetLocalDataPath();
            LocalDataSharedPath = sharedDataPath ?? GetLocalDataSharedPath();
            SharedDllPath = GetLocalSharedDllPath();
            LogPath = LocalDataPath.GetChildDirectoryWithName("Logs");
            TempPath = tempPath ??
                       Path.Combine(Path.GetTempPath(), Common.AppCommon.ApplicationName)
                           .ToAbsoluteDirectoryPath();
            ToolPath = toolPath ?? LocalDataSharedPath.GetChildDirectoryWithName("Tools");
            ToolMinGwBinPath = ToolPath.GetChildDirectoryWithName("mingw").GetChildDirectoryWithName("bin");
            ToolCygwinBinPath = ToolPath.GetChildDirectoryWithName("cygwin").GetChildDirectoryWithName("bin");
            ConfigPath = configPath ?? ToolPath.GetChildDirectoryWithName("Config");
            StartPath = Directory.GetCurrentDirectory().ToAbsoluteDirectoryPath();

            AwesomiumPath = LocalDataSharedPath.GetChildDirectoryWithName("CEF");

            MyDocumentsPath = GetMyDocumentsPath();
            ProgramDataPath = GetProgramDataPath();
            SharedFilesPath = AppPath;

            ServiceExePath = Common.IsMini ? EntryLocation : SharedFilesPath.GetChildFileWithName(ServiceExe);
            SelfUpdaterExePath = SharedFilesPath.GetChildFileWithName(SelfUpdaterExe);

            SynqRootPath = ProgramDataPath.GetChildDirectoryWithName("Synq");

            PathsSet = true;
        }

        IAbsoluteDirectoryPath GetSystemSharedPath()
            => ProgramDataPath.GetChildDirectoryWithName(CompanyPath).GetChildDirectoryWithName("Shared");

        public static IAbsoluteDirectoryPath GetLocalDataRootPath()
            => Path.Combine(GetFolderPath(EnvironmentSpecial.SpecialFolder.LocalApplicationData),
                CompanyPath).ToAbsoluteDirectoryPath();

        public static IAbsoluteDirectoryPath GetRoamingRootPath()
            => Path.Combine(GetFolderPath(EnvironmentSpecial.SpecialFolder.ApplicationData),
                CompanyPath).ToAbsoluteDirectoryPath();

        IAbsoluteDirectoryPath GetLocalDataSharedPath() => LocalDataRootPath.GetChildDirectoryWithName("Shared");

        IAbsoluteDirectoryPath GetLocalSharedDllPath() => AppPath;

        protected virtual IAbsoluteDirectoryPath GetAppPath() => CommonBase.AssemblyLoader.GetNetEntryPath();

        protected virtual IAbsoluteDirectoryPath GetDataPath()
            => GetRoamingRootPath().GetChildDirectoryWithName(Common.AppCommon.ApplicationName);

        protected virtual IAbsoluteDirectoryPath GetLocalDataPath()
            => LocalDataRootPath.GetChildDirectoryWithName(Common.AppCommon.ApplicationName);

        protected virtual IAbsoluteDirectoryPath GetMyDocumentsPath()
            => GetFolderPath(EnvironmentSpecial.SpecialFolder.MyDocuments).ToAbsoluteDirectoryPath();

        protected virtual IAbsoluteDirectoryPath GetProgramDataPath()
            => GetFolderPath(EnvironmentSpecial.SpecialFolder.CommonApplicationData).ToAbsoluteDirectoryPath();
    }
}