// <copyright company="SIX Networks GmbH" file="StatusRepo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using NDepend.Path;

using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Sync.Core.Transfer;

namespace SN.withSIX.Sync.Core.Legacy.Status
{
    
    public class StatusRepo : ModelBase, IHaveTimestamps, IDisposable
    {
        RepoStatus _action;
        long _fileSize;
        long _fileSizeNew;
        long _fileSizeTransfered;
        StatusMod _owner;
        StatusInfo _statusInfo;
        int _total = -1;

        public StatusRepo() {
            CreatedAt = Tools.Generic.GetCurrentUtcDateTime;
            UpdatedAt = Tools.Generic.GetCurrentUtcDateTime;
            CTS = new Lazy<CancellationTokenSource>();
            Info = new StatusInfo(RepoStatus.Waiting, 0, 0, 0, 0);
        }

        public CancellationToken CancelToken => CTS.Value.Token;
        public StatusInfo Info
        {
            get { return _statusInfo; }
            set { SetProperty(ref _statusInfo, value); }
        }

        public void Dispose() {
            Dispose(true);
        }

        public void UpdateTotals() {
            var items = Items.ToArrayLocked();
            var done = GetDoneCount(items);
            UpdateData(CalculateProgress(items), GetActiveItemsCount(items), done, items.Sum(x => x.Speed));
        }

        double CalculateProgress(ICollection<IStatus> items) {
            if (items.Count == 0)
                return 0;
            if (Total == -1)
                return CalculateProgressBasedOnItemProgress(items);
            if (Total < -1)
                throw new ArgumentOutOfRangeException("Total", "Less than -1");
            //return PackFolder == null || Action != RepoStatus.Downloading
            //  ? CalculateProgressBasedOnItemProgress(items) // CalculateProgressBasedOnCount(done, Total)
            //: CalculateProgressBasedOnSize(items);
            return CalculateProgressBasedOnItemProgress(items); // Basic progress for now
        }

        static double CalculateProgressBasedOnItemProgress(ICollection<IStatus> items) {
            if (items.Count == 0)
                return 0;
            return items.Sum(x => x.Progress)/items.Count;
        }

        static int GetActiveItemsCount(IEnumerable<IStatus> items) => items.Count(x => {
            var inty = (int) x.Action;
            return inty > 0 && inty < 900;
        });

        public void Finish() {
            Info = Info.Finish();
        }

        public void Reset() {
            Info = new StatusInfo(Action, 0, 0, 0, 0);
        }

        void UpdateData(double progress, int? active, int done, long? speed) {
            Info = new StatusInfo(Action, progress, speed, active, done);
        }

        static int GetDoneCount(IEnumerable<IStatus> items) => items.Count(x => x.Completed || x.Failed);

        // This does not work with rsync or casual http download, because rsync uses weird .IOJCOIJ_Q!KDp filenames, and http downloader uses .sixtmp as temporary file
        // So this is of limited use right now.
        double CalculateProgressBasedOnSize(IEnumerable<IStatus> items) {
            var totalDownloaded = ProcessStatusItems(items);
            double tmp = totalDownloaded/(float) (DownloadSize - ExistingFileSize);
            if (tmp > 1)
                tmp = 1;
            return tmp*100;
        }

        long ProcessStatusItems(IEnumerable<IStatus> items) {
            long totalDownloaded = 0;
            foreach (var status in items) {
                if (status.Action == RepoStatus.Finished)
                    totalDownloaded += status.FileSizeNew;
                else if (PackFolder != null)
                    totalDownloaded += TryGetFileLength(status);
            }
            return totalDownloaded;
        }

        long TryGetFileLength(IStatus status) {
            try {
                var location = TryGetObjectPath(status);
                if (string.IsNullOrWhiteSpace(location))
                    return 0;
                var partPath = PackFolder.GetChildFileWithName(location + ".part");
                var fullPath = PackFolder.GetChildFileWithName(location);
                var partFileExists = partPath.Exists;
                if (!partFileExists && !fullPath.Exists)
                    return 0;
                return !partFileExists
                    ? fullPath.FileInfo.Length
                    : partPath.FileInfo.Length;
            } catch (Exception) {
                // File doesn't exist, don't bother.
                return 0;
            }
        }

        static string TryGetObjectPath(IStatus status) {
            var realObject = status.RealObject;
            // Synq uses RealObject, other transfer methods have the fileName actually set as Item
            return string.IsNullOrWhiteSpace(realObject) ? status.Item : realObject.Replace("/", "\\");
        }

        protected virtual void Dispose(bool disposing) {
            var cts = CTS;
            CTS = null;
            if (cts != null && cts.IsValueCreated)
                cts.Value.Dispose();
        }

        #region StatusRepo Members

        Lazy<CancellationTokenSource> CTS;

        public StatusMod Owner
        {
            get { return _owner; }
            set
            {
                SetProperty(ref _owner, value);
                if (value != null)
                    value.Repo = this;
            }
        }

        public ObservableCollection<IStatus> Items { get; } = new ObservableCollection<IStatus>();

        public int Total
        {
            get { return _total; }
            set { SetProperty(ref _total, value); }
        }

        public long FileSize
        {
            get { return _fileSize; }
            set { SetProperty(ref _fileSize, value); }
        }

        public long FileSizeNew
        {
            get { return _fileSizeNew; }
            set { SetProperty(ref _fileSizeNew, value); }
        }

        public long FileSizeTransfered
        {
            get { return _fileSizeTransfered; }
            set { SetProperty(ref _fileSizeTransfered, value); }
        }

        public bool Aborted { get; set; }

        public long DownloadSize { get; set; }

        public IAbsoluteDirectoryPath PackFolder { get; set; }

        public long ExistingFileSize { get; set; }
        public RepoStatus Action
        {
            get { return _action; }
            set { SetProperty(ref _action, value); }
        }

        public void AddItem(IStatus item) {
            lock (Items)
                Items.Add(item);
        }

        public void AddItems(IEnumerable<IStatus> items) {
            lock (Items)
                Items.AddRange(items);
        }


        public bool Failed() => Items.Any(x => x.Failed);

        public void CalcFileSizes() {
            long fs = 0;
            long fsNew = 0;
            long fsT = 0;

            foreach (var item in Items.ToArrayLocked()) {
                fs += item.FileSize;
                fsNew += item.FileSizeNew;
                fsT += item.FileSizeTransfered;
            }

            FileSize = fs;
            FileSizeNew = fsNew;
            FileSizeTransfered = fsT;
        }


        public void IncrementDone() {
            Info = Info.IncrementDone();
        }

        public void Restart() {
            UpdateProgress(0);
        }

        public void UpdateProgress(double progress) {
            Info = Info.UpdateProgress(progress);
        }

        public void Reset(RepoStatus status, int total) {
            lock (Items)
                Items.Clear();
            ResetWithoutClearItems(status, total);
        }

        public void ResetWithoutClearItems(RepoStatus status, int total) {
            Total = total;
            Action = status;
            UpdateData(0, 0, 0, 0);
        }

        public void ProcessSize(IEnumerable<string> unchanged, IAbsoluteDirectoryPath packFolder, long downloadSize) {
            DownloadSize = downloadSize;
            PackFolder = packFolder;
            ExistingFileSize = GetExistingPackFiles(unchanged).Sum(s => s.FileInfo.Length);
        }

        IEnumerable<IAbsoluteFilePath> GetExistingPackFiles(IEnumerable<string> unchanged)
            => unchanged.Select(x => PackFolder.GetChildFileWithName(x)).Where(x => x.Exists);

        public void Abort() {
            Aborted = true;
            var cts = CTS;
            if (cts != null)
                CTS.Value.Cancel();
        }

        #endregion
    }

    public class StatusInfo : IEquatable<StatusInfo>
    {
        public StatusInfo(RepoStatus action, double progress, long? speed, int? active, int done) {
            if (progress.Equals(double.NaN))
                throw new ArgumentOutOfRangeException(nameof(progress));
            Action = action;
            Progress = progress;
            Speed = speed;
            Active = active;
            Done = done;
        }

        public RepoStatus Action { get; }
        public double Progress { get; }
        public long? Speed { get; }
        public int? Active { get; }
        public int Done { get; }

        public bool Equals(StatusInfo other)
            => other != null && other.Action == Action && other.Progress.Equals(Progress) && other.Speed == Speed &&
               other.Active == Active && other.Done == Done;

        public override int GetHashCode()
            => HashCode.Start.Hash(Action).Hash(Progress).Hash(Speed).Hash(Active).Hash(Done);

        public override bool Equals(object obj) => Equals(obj as StatusInfo);
    }

    public struct HashCode
    {
        private readonly int _hashCode;

        public HashCode(int hashCode) {
            _hashCode = hashCode;
        }

        public static HashCode Start => new HashCode(17);

        public static implicit operator int(HashCode hashCode) => hashCode.GetHashCode();

        public HashCode Hash<T>(T obj) {
            var c = EqualityComparer<T>.Default;
            var h = c.Equals(obj, default(T)) ? 0 : obj.GetHashCode();
            unchecked {
                h += _hashCode * 31;
            }
            return new HashCode(h);
        }

        public override int GetHashCode() => _hashCode;
    }

public static class StatusInfoExtensions
    {
        public static StatusInfo IncrementDone(this StatusInfo statusInfo)
            => new StatusInfo(statusInfo.Action, statusInfo.Progress, statusInfo.Speed, statusInfo.Active,
                statusInfo.Done + 1);

        public static StatusInfo UpdateProgress(this StatusInfo statusInfo, double progress)
            => new StatusInfo(statusInfo.Action, progress, statusInfo.Speed, statusInfo.Active, statusInfo.Done);

        public static StatusInfo Finish(this StatusInfo statusInfo)
            => new StatusInfo(statusInfo.Action, 100, 0, 0, statusInfo.Done);
    }

    public static class StatusRepoExtensions
    {
        public static IDisposable Monitor(this StatusRepo This, ProgressLeaf leaf, bool inclProgress = false) {
            var c = new AverageContainer2(20);
            var isZero = true;
            return new RepoWatcher(This, current => Call(leaf, inclProgress, c, current.Speed, current.Progress));
        }

        private class RepoWatcher : IDisposable
        {
            private readonly StatusRepo _repo;
            private readonly Action<StatusInfo> _action;

            public RepoWatcher(StatusRepo repo, Action<StatusInfo> action) {
                _repo = repo;
                _action = action;
                _repo.PropertyChanged += RepoOnPropertyChanged;
                _action(repo.Info);
            }

            private void RepoOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs) {
                if (propertyChangedEventArgs.PropertyName == "" || propertyChangedEventArgs.PropertyName == "Info")
                    _action(_repo.Info);
            }

            public void Dispose() => _repo.PropertyChanged -= RepoOnPropertyChanged;
        }

        private static void Call(ProgressLeaf leaf, bool inclProgress, AverageContainer2 c, long? speed, double progress) {
            leaf.Update(c.UpdateSpecial(speed), inclProgress ? progress : leaf.Progress);
        }
    }
}