// <copyright company="SIX Networks GmbH" file="GameScanner.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using withSIX.Core.Extensions;
using withSIX.Core.Helpers;
using withSIX.Core.Services;
using withSIX.Play.Core.Glue.Helpers;
using withSIX.Api.Models.Extensions;

namespace withSIX.Play.Core.Games.Legacy
{
    
    public class GameScanner : SelectionList<FoundGame>, IDomainService
    {
        readonly object _busyLock = new Object();
        readonly string[] _knownGames = {
            "arma2.exe", "arma2oa.exe", "arma3.exe", "Take on Helicopters.exe",
            "TakeOnH.exe"
        };
        bool _isBusy;
        bool _isSkipTree;
        string _status;
        public bool Verbose { get; set; }
        protected bool IsAborted { get; set; }
        public virtual string Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }
        public bool IsSkipTree
        {
            get { return _isSkipTree; }
            set { SetProperty(ref _isSkipTree, value); }
        }
        public virtual bool IsBusy
        {
            get { return _isBusy; }
            protected set { SetProperty(ref _isBusy, value); }
        }

        public void Abort() {
            IsAborted = true;
        }

        public void SkipTree() {
            IsSkipTree = true;
        }

        public Task<bool> ScanForGames() => TaskExt.StartLongRunningTask(() => ScanAllDrives());

        bool ScanAllDrives() {
            lock (_busyLock) {
                if (IsBusy)
                    return false;
                IsBusy = true;
                IsAborted = false;
            }

            Items.Clear();
            Status = "scanning...";

            foreach (var drive in DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.Fixed)) {
                if (IsAborted)
                    break;
                IsSkipTree = false;
                TryScanDrive(drive);
            }
            Status = IsAborted ? "aborted" : "all done";
            IsAborted = false;
            IsBusy = false;
            return true;
        }

        void TryScanDrive(DriveInfo drive) {
            Status = $"scanning drive {drive.RootDirectory}...";
            try {
                Scan(drive.RootDirectory);
            } catch (IOException) {}
        }

        void Scan(DirectoryInfo di) {
            if (IsAborted || IsSkipTree)
                return;

            if (Verbose)
                Status = $"scanning {di.FullName}";

            TryEnumerate(di);

            foreach (var dir in di.FilterDottedDirectories()) {
                if (IsAborted || IsSkipTree)
                    break;
                TryScanDir(dir);
            }
        }

        void TryScanDir(DirectoryInfo dir) {
            try {
                Scan(dir);
            } catch (IOException) {}
        }

        void TryEnumerate(DirectoryInfo di) {
            try {
                Items.AddRange(EnumerateKnownGames(di));
            } catch (IOException) {}
        }

        IEnumerable<FoundGame> EnumerateKnownGames(DirectoryInfo di) => di.EnumerateFiles()
    .Where(x => _knownGames.Contains(x.Name.ToLower()))
    .Select(x => new FoundGame(x.FullName));
    }
}