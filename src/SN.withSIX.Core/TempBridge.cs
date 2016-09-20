using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDepend.Path;

namespace SN.withSIX.Core
{
    public class SteamDirectories
    {
        protected SteamDirectories(SteamGameDirectories game, SteamWorkshopDirectories workshop) {
            Game = game;
            Workshop = workshop;
        }

        public SteamDirectories(uint appId, string folder, IAbsoluteDirectoryPath steamPath) {
            Contract.Requires<ArgumentNullException>(appId > 0);
            Contract.Requires<ArgumentNullException>(folder != null);
            Contract.Requires<ArgumentNullException>(steamPath != null);

            // TODO: Take LibraryPath from KV store
            RootPath = steamPath.GetChildDirectoryWithName("steamapps");
            Game = new SteamGameDirectories(folder, RootPath);
            Workshop = new SteamWorkshopDirectories(appId, RootPath);
        }

        public static SteamDirectories Default { get; } = new NullSteamDirectories();

        public IAbsoluteDirectoryPath RootPath { get; }

        public SteamGameDirectories Game { get; }

        public SteamWorkshopDirectories Workshop { get; }

        public virtual bool IsValid => true;

        private class NullSteamDirectories : SteamDirectories
        {
            protected internal NullSteamDirectories()
                : base(SteamGameDirectories.Default, SteamWorkshopDirectories.Default) { }

            public override bool IsValid => false;
        }
    }

    public class SteamWorkshopDirectories
    {
        protected SteamWorkshopDirectories() { }

        public SteamWorkshopDirectories(uint appId, IAbsoluteDirectoryPath rootPath) {
            RootPath = rootPath.GetChildDirectoryWithName("workshop");
            ContentPath = RootPath.GetChildDirectoryWithName("content").GetChildDirectoryWithName(appId.ToString());
        }

        public static SteamWorkshopDirectories Default { get; } = new SteamWorkshopDirectories();

        public IAbsoluteDirectoryPath RootPath { get; }
        public IAbsoluteDirectoryPath ContentPath { get; }
    }

    public class SteamGameDirectories
    {
        protected SteamGameDirectories() { }

        public SteamGameDirectories(string folder, IAbsoluteDirectoryPath rootPath) {
            RootPath = rootPath.GetChildDirectoryWithName("common");
            ContentPath = RootPath.GetChildDirectoryWithName(folder);
        }

        public static SteamGameDirectories Default { get; } = new SteamGameDirectories();

        public IAbsoluteDirectoryPath RootPath { get; }
        public IAbsoluteDirectoryPath ContentPath { get; }
    }

    public interface ISteamHelper
    {
        bool SteamFound { get; }
        ISteamApp TryGetSteamAppById(uint appId, bool noCache = false);
        IAbsoluteDirectoryPath SteamPath { get; }
    }


    public interface ISteamApp
    {
        uint AppId { get; }
        IAbsoluteDirectoryPath InstallBase { get; }
        IAbsoluteDirectoryPath AppPath { get; }
        bool IsValid { get; }
        string GetInstallDir();
    }


    public class SteamInitializationException : DidNotStartException
    {
        public SteamInitializationException(string message) : base(message) { }
    }

    public class SteamNotFoundException : DidNotStartException
    {
        public SteamNotFoundException(string message) : base(message) { }
    }
}
