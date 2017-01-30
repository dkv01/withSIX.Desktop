// <copyright company="SIX Networks GmbH" file="Package.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Publishing;
using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Core.Helpers;
using withSIX.Core.Logging;
using withSIX.Sync.Core.Legacy;
using withSIX.Sync.Core.Legacy.SixSync;
using withSIX.Sync.Core.Legacy.Status;
using withSIX.Sync.Core.Packages.Internals;
using withSIX.Sync.Core.Repositories.Internals;
using withSIX.Sync.Core.Services;
using withSIX.Sync.Core.Transfer;
using Repository = withSIX.Sync.Core.Repositories.Repository;

namespace withSIX.Sync.Core.Packages
{
    public class SynqSpec
    {
        public SynqSpec() {
            Processing = new Processing();
        }

        public Processing Processing { get; set; }
    }

    public class Processing
    {
        public bool? Sign { get; set; }
    }

    public class Package : IComparePK<Package>, IEnableLogging
    {
        const string SynqInfoJoiner = "\r\n";
        public const string SynqInfoFile = ".synqinfo";
        public const string SynqSpecFile = ".synqspec";
        public static readonly PackageFactory Factory = new PackageFactory();
        static readonly string[] synqInfoSeparator = { SynqInfoJoiner, "\n" };

        public Package(IAbsoluteDirectoryPath workingDirectory, string packageName, Repository repository) {
            if (workingDirectory == null) throw new ArgumentNullException(nameof(workingDirectory));
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            WorkingPath = workingDirectory;
            Repository = repository;
            ConfirmPathValidity();
            MetaData = Load(Repository.GetMetaDataPath(packageName));
            ConfirmPackageValidity(packageName);
            StatusRepo = new StatusRepo();
        }

        public Package(IAbsoluteDirectoryPath workingDirectory, PackageMetaData metaData, Repository repository) {
            if (workingDirectory == null) throw new ArgumentNullException(nameof(workingDirectory));
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            WorkingPath = workingDirectory;
            Repository = repository;
            ConfirmPathValidity();
            MetaData = metaData;
            StatusRepo = new StatusRepo();
        }

        public StatusRepo StatusRepo { get; set; }
        Repository Repository { get; }
        public IAbsoluteDirectoryPath WorkingPath { get; private set; }
        public PackageMetaData MetaData { get; private set; }

        public bool ComparePK(object other) {
            var o = other as Package;
            return (o != null) && ComparePK(o);
        }

        public bool ComparePK(Package other) => (other != null) && other.GetFullName().Equals(GetFullName());

        public static async Task<SynqSpec> ReadSynqSpecAsync(IAbsoluteDirectoryPath path) {
            var specFile = path.GetChildFileWithName(SynqSpecFile);
            return specFile.Exists
                ? await Tools.Serialization.Json.LoadJsonFromFileAsync<SynqSpec>(specFile).ConfigureAwait(false)
                : new SynqSpec();
        }

        public static SynqSpec ReadSynqSpec(IAbsoluteDirectoryPath path) {
            var specFile = path.GetChildFileWithName(SynqSpecFile);
            return specFile.Exists ? Tools.Serialization.Json.LoadJsonFromFile<SynqSpec>(specFile) : new SynqSpec();
        }

        public void SetWorkingPath(string path) {
            WorkingPath = Legacy.SixSync.Repository.RepoTools.GetRootedPath(path);
        }

        public static SpecificVersion ReadSynqInfoFile(IAbsoluteDirectoryPath packageDir) {
            if (!packageDir.Exists)
                return null;
            var file = packageDir.GetChildFileWithName(SynqInfoFile);
            return !file.Exists ? null : new SpecificVersion(File.ReadAllText(file.ToString()));
        }

        void ConfirmPackageValidity(string packageName) {
            var fn = MetaData.GetFullName();
            if (fn != packageName)
                throw new Exception("Invalid package metadata: {0} vs {1}".FormatWith(fn, packageName));
        }

        void ConfirmPathValidity() {
            Repository.Factory.HandleRepositoryRequirements(Repository.RootPath);
            Repository.Factory.HandlePackageRequirements(WorkingPath, Repository);
        }

        public string GetFullName() => MetaData.GetFullName();

        public static IAbsoluteDirectoryPath GetRepoPath(string repositoryDirectory,
            IAbsoluteDirectoryPath workingDirectory) => string.IsNullOrWhiteSpace(repositoryDirectory)
            ? workingDirectory.GetChildDirectoryWithName(Repository.DefaultRepoRootDirectory)
            : repositoryDirectory.ToAbsoluteDirectoryPath();

        public bool Commit(string desiredVersion, bool force = false, bool downCase = true) {
            var metaData = CreateUpdatedMetaData(desiredVersion, downCase);

            if (!force && MetaData.Compare(metaData))
                return false;

            MetaData = metaData;
            Save();
            return true;
        }

        public Task<Guid> Register(IPublishingApi api, string registerInfo)
            => api.Publish(BuildPublishModel(registerInfo));

        public Task<Guid> DeRegister(IPublishingApi api, string registerInfo)
            => api.Publish(BuildPublishModel(registerInfo));

        PublishModModel BuildPublishModel(string registerInfo) {
            var yml = WorkingPath.GetChildDirectoryWithName(".rsync\\.pack")
                .GetChildFileWithName(".repository.yml");

            var cppInfo = GetCppInfo();
            return new PublishModModel {
                PackageName = MetaData.Name,
                Revision = yml.Exists
                    ? (int) SyncEvilGlobal.Yaml.NewFromYamlFile<RepoVersion>(yml).Version
                    : 0,
                Version = MetaData.GetVersionInfo(),
                Size = MetaData.SizePacked,
                SizeWd = MetaData.Size,
                Readme = GetReadme(),
                Changelog = GetChangelog(),
                License = GetLicense(),
                CppName = cppInfo.Item1,
                Description = cppInfo.Item2.TruncateNullSafe(500),
                Author = cppInfo.Item3,
                RegisterInfo = registerInfo
            };
        }

        Tuple<string, string, string> GetCppInfo() {
            var cppFile = WorkingPath.GetChildFileWithName("mod.cpp");
            if (!cppFile.Exists)
                return new Tuple<string, string, string>(null, null, null);
            var fileContent = File.ReadAllText(cppFile.ToString());

            var p = new ModCppParser(fileContent);
            var name = p.GetName();
            var description = p.GetDescription();
            var author = p.GetAuthor();

            return Tuple.Create(name, description, author);
        }

        string GetChangelog() {
            var text = "";
            foreach (var cl in WorkingPath.DirectoryInfo.EnumerateFiles("*changelog*.txt", SearchOption.AllDirectories)) {
                text += cl.Name + "\n\n";
                text += File.ReadAllText(cl.FullName);
            }
            return text.Length == 0 ? null : text;
        }

        string GetLicense() {
            var text = "";
            foreach (var cl in WorkingPath.DirectoryInfo.EnumerateFiles("*license*.txt", SearchOption.AllDirectories)) {
                text += cl.Name + "\n\n";
                text += File.ReadAllText(cl.FullName);
            }
            return text.Length == 0 ? null : text;
        }

        string GetReadme() {
            var text = "";
            foreach (var txt in WorkingPath.DirectoryInfo.EnumerateFiles("*.txt", SearchOption.AllDirectories)
                .Where(x => !x.Name.ContainsIgnoreCase("changelog") && !x.Name.ContainsIgnoreCase("license"))) {
                text += txt.Name + "\n\n";
                text += File.ReadAllText(txt.FullName);
            }
            return text.Length == 0 ? null : text;
        }

        PackageMetaData CreateUpdatedMetaData(string desiredVersion, bool downCase)
            => UpdateMetaData(downCase, MetaData.SpawnNewVersion(desiredVersion));

        PackageMetaData UpdateMetaData(bool downCase, PackageMetaData metaData) {
            metaData.Files = Repository.Commit(WorkingPath, downCase);
            var paths = new List<string>();
            GetMetaDataFilesOrderedBySize(metaData).ForEach(x => UpdateFileMetaData(metaData, x, paths));

            if (string.IsNullOrWhiteSpace(metaData.ContentType))
                return metaData;
            Repository.SetContentType(metaData.Name, metaData.ContentType);
            Repository.Save();
            // TODO: This should be done in one go, now its done twice once at Repository.Commit and once here :S

            return metaData;
        }

        void UpdateFileMetaData(PackageMetaData metaData, FileObjectMapping x, ICollection<string> paths) {
            metaData.Size += new FileInfo(Path.Combine(WorkingPath.ToString(), x.FilePath)).Length;
            var path = Repository.GetObjectPath(x.Checksum);
            if (paths.Contains(path.ToString()))
                return;
            paths.Add(path.ToString());
            metaData.SizePacked += new FileInfo(path.ToString()).Length;
        }

        public bool Commit(bool force = false, bool downCase = true) => Commit(
            new Dependency(MetaData.Name, MetaData.Version.AutoIncrement().ToString(), MetaData.Branch)
                .VersionData,
            force, downCase);

        public static PackageMetaData TryLoad(IAbsoluteFilePath metaDataPath) {
            try {
                return Repository.Load<PackageMetaDataDto, PackageMetaData>(metaDataPath);
            } catch (Exception) {
                return null;
            }
        }

        public static PackageMetaData Load(IAbsoluteFilePath metaDataPath)
            => Repository.Load<PackageMetaDataDto, PackageMetaData>(metaDataPath);

        Task SaveAsync() => Repository.SavePackageAsync(this);

        [Obsolete("Use SaveAsync")]
        void Save() {
            Repository.SavePackage(this);
        }

        public async Task<string[]> Update(IEnumerable<Uri> remotes, StatusRepo repo, bool skipWhenFileMatches = true) {
            var objects = GetNeededObjects(skipWhenFileMatches);
            var doneObjects = new ConcurrentBag<FileObjectMapping>();
            var relObjects = objects.OrderByDescending(x => Tools.FileUtil.SizePrediction(x.FO.FilePath))
                .Select(
                    x => new FileFetchInfo(Repository.GetObjectSubPath(x.FO), x.FO.FilePath) {
                        OnComplete = () => doneObjects.Add(x.FO)
                    })
                .ToArray();
            StatusRepo.ProcessSize(GetExistingObjects(objects.Select(x => x.FO)), Repository.ObjectsPath,
                MetaData.SizePacked);

            // TODO: Abort support!
            // TODO: Progress fix??!
            try {
                await DownloadObjects(remotes, repo, relObjects, Repository.ObjectsPath).ConfigureAwait(false);
            } finally {
                Repository.ReAddObject(doneObjects.Select(x => x.Checksum).ToArray());
            }

            return relObjects.Select(x => x.FilePath).ToArray();
        }

        public Task CheckoutAsync(ProgressLeaf progressLeaf, bool confirmChecksums = true)
            => TaskExt.StartLongRunningTask(() => Checkout(progressLeaf, confirmChecksums));

        public Task CheckoutWithoutRemovalAsync(ProgressLeaf progressLeaf)
            => TaskExt.StartLongRunningTask(() => CheckoutWithoutRemoval(progressLeaf));

        public void Checkout(ProgressLeaf progressLeaf, bool confirmChecksums = true) {
            WorkingPath.MakeSurePathExists();
            ProcessCheckout(progressLeaf, true, confirmChecksums);
            WriteTag();
        }

        public void CheckoutWithoutRemoval(ProgressLeaf progressLeaf) {
            OnCheckoutWithoutRemoval(progressLeaf);
            UpdateTag();
        }

        void UpdateTag() {
            var tagFile = GetSynqInfoPath();

            var existing = GetInstalledPackages(tagFile);
            Tools.FileUtil.Ops.CreateText(tagFile,
                string.Join(SynqInfoJoiner, existing.Where(x => x.Name != MetaData.Name)
                    .Concat(new[] {new SpecificVersion(MetaData.GetFullName())})
                    .OrderBy(x => x.Name)));
        }

        public static IEnumerable<SpecificVersion> GetInstalledPackages(IAbsoluteDirectoryPath path)
            => GetInstalledPackages(path.GetChildFileWithName(SynqInfoFile));

        public static IEnumerable<SpecificVersion> GetInstalledPackages(IAbsoluteFilePath tagFile) => tagFile.Exists
            ? ReadTagFile(tagFile).Select(x => new SpecificVersion(x))
            : Enumerable.Empty<SpecificVersion>();

        static IEnumerable<string> ReadTagFile(IAbsoluteFilePath tagFile)
            => Tools.FileUtil.Ops.ReadTextFile(tagFile).Split(synqInfoSeparator, StringSplitOptions.None);

        void OnCheckoutWithoutRemoval(ProgressLeaf progressLeaf) {
            WorkingPath.MakeSurePathExists();
            ProcessCheckout(progressLeaf, false);
        }

        public Package.ObjectMap[] GetNeededObjects(bool skipWhenFileMatches = true) {
            var objects = GetMetaDataFilesOrderedBySize().ToList();

            var validObjects = new List<FileObjectMapping>();
            foreach (var o in objects)
                ProcessObject(skipWhenFileMatches, o, validObjects);

            objects.RemoveAll(validObjects);

            var newObjects = objects.Select(x => new Package.ObjectMap(x)).ToArray();

            var missingObjects = GetMissingObjectMapping(newObjects).ToList();
            if (missingObjects.Any())
                HandleMissingObjects(missingObjects);

            Repository.Log("Local object matches {0}, left: {1}", MetaData.Files.Count - objects.Count, objects.Count);

            return newObjects;
        }

        IEnumerable<string> GetExistingObjects(IEnumerable<FileObjectMapping> objects)
            => GetMetaDataFilesOrderedBySize()
                .Select(x => x.Checksum)
                .Except(objects.Select(x => x.Checksum))
                .Select(x => Repository.GetObjectSubPath(x));

        public static Task DownloadObjects(IEnumerable<Uri> remotes, StatusRepo sr,
                IEnumerable<FileFetchInfo> files, IAbsoluteDirectoryPath destination)
            => SyncEvilGlobal.DownloadHelper.DownloadFilesAsync(GetObjectRemotes(remotes).ToArray(), sr,
                GetTransferDictionary(sr, files),
                destination);

        static IEnumerable<Uri> GetObjectRemotes(IEnumerable<Uri> remotes)
            => remotes.Select(x => Tools.Transfer.JoinUri(x, Repository.ObjectsDirectory));

        static IDictionary<FileFetchInfo, ITransferStatus> GetTransferDictionary(
            StatusRepo sr,
            IEnumerable<FileFetchInfo> files) => files.OrderByDescending(x => Tools.FileUtil.SizePrediction(x.FilePath))
            .ToDictionary(x => x,
                x =>
                    (ITransferStatus)
                    new Status(x.DisplayName, sr) {RealObject = x.FilePath, OnComplete = x.Complete});

        void HandleMissingObjects(List<Package.ObjectMap> missingObjects) {
            var currentPackage = MetaData.GetFullName();
            var packages = Repository.GetPackagesList()
                .Where(x => !x.Equals(currentPackage))
                .OrderByDescending(x => x.StartsWith(MetaData.Name)).ToArray();
            if (packages.Any())
                ProcessMissingObjects(missingObjects, packages);

            var resolvableObjects = missingObjects.Where(x => x.ExistingObject != null).ToArray();
            StatusRepo.Reset(RepoStatus.Copying, resolvableObjects.Length);
            foreach (var o in resolvableObjects) {
                this.Logger()
                    .Info("Found local previous version match for {0}", o.FO.FilePath,
                        o.ExistingObject, o.FO.Checksum);
                missingObjects.Remove(o);
            }

            StatusRepo.Reset(RepoStatus.Packing, missingObjects.Count);

            var resolvedObjects = new List<Package.ObjectMap>();
            foreach (var o in missingObjects)
                ProcessMissingObject(o, resolvedObjects);
            Repository.ReAddObject(resolvedObjects.Select(x => x.ExistingObject).ToArray());

            foreach (var o in resolvedObjects)
                missingObjects.Remove(o);

            Repository.Log(
                "\nFound {0} missing objects, resolved {1} candidates from other packages and {2} from uncompressed files",
                missingObjects.Count + resolvedObjects.Count + resolvableObjects.Length, resolvableObjects.Length,
                resolvedObjects.Count);
        }

        IEnumerable<Package.ObjectMap> GetMissingObjectMapping(IEnumerable<Package.ObjectMap> objects)
            => objects.Select(o => new {o, f = Repository.GetObjectPath(o.FO)})
                .Where(t => !t.f.Exists)
                .Select(t => t.o);

        void ProcessMissingObjects(IReadOnlyCollection<Package.ObjectMap> missingObjects, string[] packages) {
            var cache = new Dictionary<string, PackageMetaData>();
            foreach (var missing in missingObjects) {
                foreach (var package in packages)
                    ProcessMissingObjects(cache, package, missing);
            }
        }

        void ProcessMissingObjects(IDictionary<string, PackageMetaData> cache, string package, Package.ObjectMap missing) {
            var metadata = RetrieveMetaData(cache, package);
            if ((metadata == null) || !metadata.Files.ContainsKey(missing.FO.FilePath))
                return;

            var match = metadata.Files[missing.FO.FilePath];
            var oPath = Repository.GetObjectPath(match);
            if (oPath.Exists)
                missing.ExistingObject = match;
        }

        PackageMetaData RetrieveMetaData(IDictionary<string, PackageMetaData> cache, string package) {
            if (cache.ContainsKey(package))
                return cache[package];

            var packageMetadataPath = GetPackageMetadataPath(package);
            var metadata = !packageMetadataPath.Exists
                ? null
                : TryLoad(packageMetadataPath);
            cache.Add(package, metadata);
            return metadata;
        }

        IAbsoluteFilePath GetPackageMetadataPath(string package)
            => Repository.PackagesPath.GetChildFileWithName(package + Repository.PackageFormat);

        static string GetObjectPathFromChecksum(FileObjectMapping fileObjectMapping)
            => "objects/" + fileObjectMapping.Checksum.Substring(0, 2) + "/" +
               fileObjectMapping.Checksum.Substring(2);

        void ProcessMissingObject(Package.ObjectMap o, ICollection<Package.ObjectMap> resolvedObjects) {
            var f = WorkingPath.GetChildFileWithName(o.FO.FilePath);
            if (!f.Exists)
                return;
            var status = new Status(o.FO.FilePath, StatusRepo) {
                Action = RepoStatus.Packing,
                RealObject = GetObjectPathFromChecksum(o.FO)
            };
            var checksum = Repository.GetChecksum(f);
            this.Logger().Info("Found local previous version file for {0}. Compressing to {1}", o.FO.FilePath,
                checksum);
            Repository.CompressObject(f, checksum);
            o.ExistingObject = checksum;
            resolvedObjects.Add(o);
            status.EndOutput();
        }

        void ProcessObject(bool skipWhenLocalFileMatches, FileObjectMapping o,
            ICollection<FileObjectMapping> validObjects) {
            if (skipWhenLocalFileMatches) {
                // We can also skip objects that already match in the working directory so that we don't waste time on compressing or copying objects needlessly
                // this however could create more bandwidth usage in case the user in the future deletes working files, and tries to get the version again
                // in that case the objects will need to be redownloaded, or at least patched up from other possible available objects.
                var path = WorkingPath.GetChildFileWithName(o.FilePath);
                if (path.Exists
                    && Repository.GetChecksum(path).Equals(o.Checksum)) {
                    validObjects.Add(o);
                    if (Common.Flags.Verbose) {
                        MainLog.Logger.Info(
                            $"Marking {o.FilePath} ({o.Checksum}) as valid, because the local object matches");
                    }
                    return;
                }

                var oPath = Repository.GetObjectPath(o.Checksum);
                if (oPath.Exists) {
                    validObjects.Add(o);
                    if (Common.Flags.Verbose) {
                        MainLog.Logger.Info(
                            $"Marking {o.FilePath} ({o.Checksum}) as valid, because the packed object exists");
                    }
                    // Don't readd object because we can't validate if the Checksum is in order..
                }
            } else {
                var ob = Repository.GetObject(o.Checksum);
                if (ob == null)
                    return;
                var oPath = Repository.GetObjectPath(o.Checksum);
                if (oPath.Exists && Repository.GetChecksum(oPath).Equals(ob.ChecksumPack)) {
                    validObjects.Add(o);
                    if (Common.Flags.Verbose) {
                        MainLog.Logger.Info(
                            $"Marking {o.FilePath} ({o.Checksum}) as valid, because the packed object matches");
                    }
                }
            }
        }

        void ProcessCheckout(ProgressLeaf progressLeaf, bool withRemoval = true, bool confirmChecksums = true) {
            HandleDownCase();
            var mappings = GetMetaDataFilesOrderedBySize();
            var changeAg = GetInitialChangeList(withRemoval, mappings);

            TryCrippleSixSyncIfExists();

            if (withRemoval)
                HandleChangesWithRemove(mappings, changeAg, progressLeaf);
            else
                HandleChanges(mappings, changeAg, progressLeaf);

            if (confirmChecksums) ConfirmChanges(withRemoval, mappings);
        }

        void HandleDownCase() {
            Tools.FileUtil.HandleDowncaseFolder(WorkingPath);
        }

        Package.ChangeList GetInitialChangeList(bool withRemoval, IOrderedEnumerable<FileObjectMapping> mappings) {
            var workingPathFiles = GetWorkingPathFiles(withRemoval, mappings);
            var changeAg = new Package.ChangeList(workingPathFiles, mappings, this);

            PrintChangeOverview(workingPathFiles, mappings);
            Console.WriteLine();
            PrintDetailedChanges(changeAg, withRemoval);

            return changeAg;
        }

        // TODO: Make this a Verifying progress state?
        void ConfirmChanges(bool withRemoval, IOrderedEnumerable<FileObjectMapping> mappings) {
            var afterChangeAg = new Package.ChangeList(GetWorkingPathFiles(withRemoval, mappings), mappings, this);
            if (!afterChangeAg.HasChanges(withRemoval))
                return;
            PrintDetailedChanges(afterChangeAg, withRemoval);
            throw new ChecksumException("See log for details");
        }

        void TryCrippleSixSyncIfExists() {
            try {
                CrippleSixSyncIfExists();
            } catch (IOException e) {
                this.Logger().FormattedWarnException(e, "failure to clear legacy .rsync/.repository.yml");
            }
        }

        void CrippleSixSyncIfExists() {
            Tools.FileUtil.Ops.DeleteIfExists(Path.Combine(WorkingPath.ToString(),
                Legacy.SixSync.Repository.RepoFolderName,
                ".repository.yml"));
        }

        public IOrderedEnumerable<FileObjectMapping> GetMetaDataFilesOrderedBySize()
            => GetMetaDataFilesOrderedBySize(MetaData);

        static IOrderedEnumerable<FileObjectMapping> GetMetaDataFilesOrderedBySize(PackageMetaData metaData)
            => metaData.GetFiles()
                .OrderByDescending(x => Tools.FileUtil.SizePrediction(x.FilePath));

        IAbsoluteFilePath[] GetWorkingPathFiles(bool withRemoval, IOrderedEnumerable<FileObjectMapping> mappings) {
            if (!withRemoval) {
                return
                    mappings.Select(x => WorkingPath.GetChildFileWithName(x.FilePath))
                        .Where(x => x.Exists).ToArray();
            }

            var files = Repository.GetFiles(WorkingPath);
            return files
                .OrderByDescending(x => Tools.FileUtil.SizePrediction(x.FileName)).ToArray();
        }

        void HandleChanges(IEnumerable<FileObjectMapping> mappings, Package.ChangeList changeAg, ProgressLeaf progressLeaf) {
            HandleCopy(changeAg.Copy, changeAg.StatusDic);
            HandleChangedCase(changeAg.ChangedCase, changeAg.StatusDic);
            HandleModify(Convert(mappings, changeAg.GetModified()).ToArray(), changeAg.StatusDic, progressLeaf);
        }

        void HandleChangesWithRemove(IEnumerable<FileObjectMapping> mappings, Package.ChangeList changeAg,
            ProgressLeaf progressLeaf) {
            HandleCopy(changeAg.Copy, changeAg.StatusDic);
            HandleRemove(changeAg.Remove, changeAg.StatusDic);
            HandleChangedCase(changeAg.ChangedCase, changeAg.StatusDic);
            HandleModify(Convert(mappings, changeAg.GetModified()).ToArray(), changeAg.StatusDic, progressLeaf);
        }

        private static IEnumerable<FileObjectMapping> Convert(IEnumerable<FileObjectMapping> mappings,
            IEnumerable<string> changes) => changes.Select(x => mappings.Single(y => y.FilePath.Equals(x)));

        void HandleChangedCase(ICollection<KeyValuePair<string, string>> changedCase,
            IDictionary<string, Status> statusDic) {
            StatusRepo.ResetWithoutClearItems(RepoStatus.Renaming, changedCase.Count);
            changedCase.ForEach(x => RenameExistingObject(statusDic, x.Value, x.Key));
        }

        void RenameExistingObject(IDictionary<string, Status> statusDic, string destName, string srcName) {
            var status = statusDic[destName];
            status.Progress = 0;
            status.Action = RepoStatus.Renaming;
            RenameFilePath(WorkingPath.GetChildFileWithName(srcName), WorkingPath.GetChildFileWithName(destName));
            status.EndOutput();
        }

        static void RenameFilePath(IAbsoluteFilePath srcFile, IAbsoluteFilePath destFile) {
            destFile.ParentDirectoryPath.MakeSurePathExists();
            Tools.FileUtil.Ops.Move(srcFile, destFile);
            RenameDirectoryPath(srcFile.ParentDirectoryPath, destFile.ParentDirectoryPath);
        }

        // TODO: It is kinda bad to go deeper than the subfolders/files of the WorkingPath
        // although currently it is no problem; the WorkingPath root is the same for both source and dest
        // it seems somewhat dangerous and we should rather not go into rename operations deeper into the workingpath
        static void RenameDirectoryPath(IAbsoluteDirectoryPath srcDir, IAbsoluteDirectoryPath dstDir) {
            var srcParts = srcDir.ToString().Split('/', '\\');
            var destParts = dstDir.ToString().Split('/', '\\');
            foreach (var paths in Enumerable.Range(0, destParts.Length).Reverse()
                .Where(i => srcParts.Length > i)
                .Select(
                    i =>
                        new {
                            src = string.Join("\\", srcParts.Take(i + 1)),
                            dst = string.Join("\\", destParts.Take(i + 1))
                        })
                .Where(
                    paths =>
                        (paths.src != paths.dst) &&
                        paths.src.Equals(paths.dst, StringComparison.OrdinalIgnoreCase))) {
                Tools.FileUtil.Ops.MoveDirectory(paths.src.ToAbsoluteDirectoryPath(),
                    paths.dst.ToAbsoluteDirectoryPath());
            }
        }

        void PrintChangeOverview(IEnumerable<IAbsoluteFilePath> files, IEnumerable<FileObjectMapping> mappings) {
            var overview = new StringBuilder();
            var full = new StringBuilder();
            BuildShortLogInfo("Current files", files.Select(x => x.FileName), overview, full);
            BuildShortLogInfo("Needed files", mappings.Select(x => x.FilePath), overview, full);

            this.Logger().Info(full.ToString());
            Repository.Log(overview.ToString());
        }

        void PrintDetailedChanges(Package.ChangeList changeAg, bool withRemoval) {
            var overview = new StringBuilder();
            var full = new StringBuilder();
            BuildLogInfos(changeAg.Equal, overview, full, changeAg.Copy, changeAg.Update,
                withRemoval ? changeAg.Remove : new List<string>(), changeAg.New, changeAg.ChangedCase);
            this.Logger().Info(full.ToString());
            Repository.Log(overview.ToString());
        }

        static void BuildLogInfos(IEnumerable<string> equal, StringBuilder overview, StringBuilder full,
            IEnumerable<KeyValuePair<string, List<string>>> copy, IReadOnlyCollection<string> update,
            IReadOnlyCollection<string> remove,
            IReadOnlyCollection<string> @new, IEnumerable<KeyValuePair<string, string>> caseChange) {
            BuildShortLogInfo("Equal", equal, overview, full);
            BuildLogInfo("CaseChange", caseChange.Select(x => x.Key + ": " + x.Value).ToList(), overview, full);
            BuildLogInfo("Copy", copy.Select(x => x.Key + ": " + string.Join(",", x.Value)).ToList(), overview, full);
            BuildLogInfo("Update", update, overview, full);
            BuildLogInfo("Remove", remove, overview, full);
            BuildLogInfo("New", @new, overview, full);
        }

        static void BuildShortLogInfo(string type, IEnumerable<string> changes, StringBuilder overview,
            StringBuilder full) {
            var info = ShortStatusInfo(type, changes);
            overview.AppendLine(info);
            full.AppendLine(info);
        }

        void HandleRemove(List<string> remove, IDictionary<string, Status> statusDic) {
            StatusRepo.ResetWithoutClearItems(RepoStatus.Removing, remove.Count());

            remove.ForEach(x => ProcessRemoved(statusDic, x));

            foreach (var d in GetEmptyDirectories(remove)) {
                Tools.FileUtil.Ops.DeleteDirectory(d);
                StatusRepo.IncrementDone();
            }
        }

        void HandleModify(ICollection<FileObjectMapping> modify, IDictionary<string, Status> statusDic,
            ProgressLeaf progressLeaf) {
            StatusRepo.ResetWithoutClearItems(RepoStatus.Unpacking, modify.Count);
            var i = 0;
            modify.OrderByDescending(x => Tools.FileUtil.SizePrediction(x.FilePath))
                .ForEach(m => {
                    ProcessModified(statusDic, m,
                        (p, s) => progressLeaf?.Update(null, (i + p/100).ToProgress(modify.Count)));
                    StatusRepo.IncrementDone();
                    progressLeaf?.Update(null, i++.ToProgress(modify.Count));
                });
            progressLeaf?.Finish();
        }

        void HandleCopy(ICollection<KeyValuePair<string, List<string>>> copy, IDictionary<string, Status> statusDic) {
            StatusRepo.ResetWithoutClearItems(RepoStatus.Copying, copy.Count);
            copy.ForEach(x => {
                x.Value.ForEach(y => CopyExistingWorkingFile(statusDic, y, x.Key));
                StatusRepo.IncrementDone();
            });
        }

        IEnumerable<IAbsoluteDirectoryPath> GetEmptyDirectories(IEnumerable<string> remove)
            => remove.Select(Path.GetDirectoryName)
                .Distinct()
                .Select(x => WorkingPath.GetChildDirectoryWithName(x))
                .Where(x => Tools.FileUtil.IsDirectoryEmpty(x));

        void ProcessRemoved(IDictionary<string, Status> statusDic, string x) {
            var status = statusDic[x];
            status.Progress = 0;
            status.Action = RepoStatus.Removing;
            Tools.FileUtil.Ops.DeleteWithRetry(WorkingPath.GetChildFileWithName(x).ToString());
            status.EndOutput();
        }

        void ProcessModified(IDictionary<string, Status> statusDic, FileObjectMapping fcm, Action<double, long?> act) {
            var status = statusDic[fcm.FilePath];
            status.Progress = 0;
            status.Action = RepoStatus.Unpacking;
            var destFile = WorkingPath.GetChildFileWithName(fcm.FilePath);
            var packedFile = Repository.GetObjectPath(fcm.Checksum);
            destFile.ParentDirectoryPath.MakeSurePathExists();

            Tools.Compression.Gzip.UnpackSingleGzip(packedFile, destFile, new StatusWrapper(status, act));
            status.EndOutput();
        }

        void CopyExistingWorkingFile(IDictionary<string, Status> statusDic, string destName, string srcName) {
            var status = statusDic[destName];
            status.Progress = 0;
            status.Action = RepoStatus.Copying;
            var srcFile = WorkingPath.GetChildFileWithName(srcName);
            var destFile = WorkingPath.GetChildFileWithName(destName);
            destFile.ParentDirectoryPath.MakeSurePathExists();
            Tools.FileUtil.Ops.CopyWithRetry(srcFile, destFile);
            status.EndOutput();
        }

        public void WriteTag() => Tools.FileUtil.Ops.CreateText(GetSynqInfoPath(), MetaData.GetFullName());

        IAbsoluteFilePath GetSynqInfoPath() => WorkingPath.GetChildFileWithName(SynqInfoFile);

        static void BuildLogInfo(string type, IReadOnlyCollection<string> changes, StringBuilder overview,
            StringBuilder full) {
            overview.AppendLine(ShortStatusInfo(type, changes));
            full.AppendLine(FullChangeInfo(type, changes));
        }

        static string FullChangeInfo(string type, IReadOnlyCollection<string> changes)
            => string.Format("{0} ({2}): {1}", type, string.Join(",", changes), changes.Count);

        static string ShortStatusInfo(string type, IEnumerable<string> changes) => $"{type}: {changes.Count()}";


        public class ObjectMap
        {
            public ObjectMap(FileObjectMapping fo) {
                FO = fo;
            }

            public FileObjectMapping FO { get; }
            public string ExistingObject { get; set; }
        }

        class ChangeList
        {
            public ChangeList(IReadOnlyCollection<IAbsoluteFilePath> workingPathFiles,
                IOrderedEnumerable<FileObjectMapping> mappings,
                Package package) {
                package.StatusRepo.Reset(RepoStatus.Summing, workingPathFiles.Count);

                GenerateChangeImage(workingPathFiles,
                    mappings.OrderByDescending(x => Tools.FileUtil.SizePrediction(x.FilePath)), package);
            }

            public IDictionary<string, string> ChangedCase { get; } = new Dictionary<string, string>();
            public IDictionary<string, List<string>> Copy { get; } = new Dictionary<string, List<string>>();
            public List<string> Equal { get; } = new List<string>();
            public List<string> New { get; } = new List<string>();
            public List<string> Remove { get; } = new List<string>();
            public IDictionary<string, Status> StatusDic { get; } = new Dictionary<string, Status>();
            public List<string> Update { get; } = new List<string>();

            public IEnumerable<string> GetModified()
                => New.Concat(Update).OrderByDescending(x => Tools.FileUtil.SizePrediction(x));

            public bool HasChanges(bool withRemoval)
                => (withRemoval && Remove.Any()) || Update.Any() || New.Any() || Copy.Any() || ChangedCase.Any();

            void GenerateChangeImage(IReadOnlyCollection<IAbsoluteFilePath> workingPathFiles,
                IOrderedEnumerable<FileObjectMapping> packageFileMappings,
                Package package) {
                var changeDictionary = new Dictionary<string, string>();

                foreach (var file in workingPathFiles)
                    MakeChecksum(package, file, changeDictionary);

                foreach (var f in changeDictionary)
                    EnumerateRemovals(f.Key, packageFileMappings);

                foreach (var file in packageFileMappings)
                    EnumerateChanges(file, changeDictionary, package);

                foreach (var file in ChangedCase.Where(file => Remove.Contains(file.Key)).ToArray())
                    Remove.Remove(file.Key);
            }

            void MakeChecksum(Package package, IAbsoluteFilePath file, Dictionary<string, string> changeDictionary) {
                var f = file.ToString().Replace(package.WorkingPath + @"\", string.Empty).Replace(@"\", "/");
                var status = new Status(f, package.StatusRepo) {Action = RepoStatus.Summing};
                StatusDic[f] = status;
                changeDictionary.Add(f, package.Repository.GetChecksum(file));
                status.EndOutput();
            }

            void EnumerateChanges(FileObjectMapping file, Dictionary<string, string> changeDictionary, Package package) {
                if (!StatusDic.ContainsKey(file.FilePath))
                    CreateStatusObject(file, package);

                var found = changeDictionary.FirstOrDefault(x => x.Key.Equals(file.FilePath));
                if (found.Key != null)
                    ProcessFoundByFilePath(file, found);
                else
                    ProcessNotFoundByFilePath(file, changeDictionary);
            }

            void CreateStatusObject(FileObjectMapping file, Package package) {
                StatusDic[file.FilePath] = new Status(file.FilePath, package.StatusRepo) {
                    RealObject = Package.GetObjectPathFromChecksum(file)
                };
            }

            void ProcessFoundByChecksum(FileObjectMapping file, string found) {
                if (found.Equals(file.FilePath, StringComparison.OrdinalIgnoreCase))
                    ChangedCase.Add(found, file.FilePath);
                else if (Copy.ContainsKey(found))
                    Copy[found].Add(file.FilePath);
                else
                    Copy.Add(found, new List<string> {file.FilePath});
            }

            void ProcessNotFoundByFilePath(FileObjectMapping file, Dictionary<string, string> changeDictionary) {
                var found = changeDictionary.FirstOrDefault(x => x.Value.Equals(file.Checksum));
                if (found.Key != null)
                    ProcessFoundByChecksum(file, found.Key);
                else
                    New.Add(file.FilePath);
            }

            void ProcessFoundByFilePath(FileObjectMapping file, KeyValuePair<string, string> found) {
                if (found.Value.Equals(file.Checksum))
                    Equal.Add(file.FilePath);
                else
                    Update.Add(file.FilePath);
            }

            void EnumerateRemovals(string f, IEnumerable<FileObjectMapping> mappings) {
                var o = mappings.FirstOrDefault(x => x.FilePath.Equals(f));
                if (o == null)
                    Remove.Add(f);
            }
        }
    }

    public class StatusWrapper2 : StatusWrapper, ITransferStatus
    {
        private readonly ITransferStatus _wrapped;

        public StatusWrapper2(ITransferStatus wrapped, Action<double, long?> act) : base(wrapped, act) {
            _wrapped = wrapped;
        }

        public TimeSpan? Eta
        {
            get { return _wrapped.Eta; }
            set { _wrapped.Eta = value; }
        }
        public bool Completed
        {
            get { return _wrapped.Completed; }
            set { _wrapped.Completed = value; }
        }
        public string Output => _wrapped.Output;
        public long FileSizeTransfered
        {
            get { return _wrapped.FileSizeTransfered; }
            set { _wrapped.FileSizeTransfered = value; }
        }
        public string ZsyncLoopData
        {
            get { return _wrapped.ZsyncLoopData; }
            set { _wrapped.ZsyncLoopData = value; }
        }
        public int ZsyncLoopCount
        {
            get { return _wrapped.ZsyncLoopCount; }
            set { _wrapped.ZsyncLoopCount = value; }
        }
        public bool ZsyncIncompatible
        {
            get { return _wrapped.ZsyncIncompatible; }
            set { _wrapped.ZsyncIncompatible = value; }
        }
        public bool ZsyncHttpFallback
        {
            get { return _wrapped.ZsyncHttpFallback; }
            set { _wrapped.ZsyncHttpFallback = value; }
        }
        public int ZsyncHttpFallbackAfter
        {
            get { return _wrapped.ZsyncHttpFallbackAfter; }
            set { _wrapped.ZsyncHttpFallbackAfter = value; }
        }
        public int Tries
        {
            get { return _wrapped.Tries; }
            set { _wrapped.Tries = value; }
        }

        public void UpdateOutput(string data) {
            _wrapped.UpdateOutput(data);
        }

        public void ResetZsyncLoopInfo() {
            _wrapped.ResetZsyncLoopInfo();
        }

        public RepoStatus Action
        {
            get { return _wrapped.Action; }
            set { _wrapped.Action = value; }
        }
        public TimeSpan? TimeTaken
        {
            get { return _wrapped.TimeTaken; }
            set { _wrapped.TimeTaken = value; }
        }
        public bool Failed
        {
            get { return _wrapped.Failed; }
            set { _wrapped.Failed = value; }
        }
        public string Item
        {
            get { return _wrapped.Item; }
        }
        public string Info
        {
            get { return _wrapped.Info; }
            set { _wrapped.Info = value; }
        }
        public string FileStatus
        {
            get { return _wrapped.FileStatus; }
            set { _wrapped.FileStatus = value; }
        }
        public string ProcessCl
        {
            get { return _wrapped.ProcessCl; }
            set { _wrapped.ProcessCl = value; }
        }
        public long FileSize
        {
            get { return _wrapped.FileSize; }
            set { _wrapped.FileSize = value; }
        }
        public long FileSizeNew
        {
            get { return _wrapped.FileSizeNew; }
            set { _wrapped.FileSizeNew = value; }
        }
        public DateTime CreatedAt
        {
            get { return _wrapped.CreatedAt; }
            set { _wrapped.CreatedAt = value; }
        }
        public DateTime? UpdatedAt
        {
            get { return _wrapped.UpdatedAt; }
            set { _wrapped.UpdatedAt = value; }
        }
        public void Reset(RepoStatus action) => _wrapped.Reset(action);

        public void Reset() => _wrapped.Reset();

        public void UpdateStamp() {
            _wrapped.UpdateStamp();
        }

        public void UpdateTimeTaken() {
            _wrapped.UpdateTimeTaken();
        }

        public void Fail() {
            _wrapped.Fail();
        }

        public void FailOutput() {
            _wrapped.FailOutput();
        }

        public void EndOutput() {
            _wrapped.EndOutput();
        }

        public void EndOutput(string f) {
            _wrapped.EndOutput(f);
        }

        public void FailOutput(string f) {
            _wrapped.FailOutput(f);
        }

        public void StartOutput(string f) {
            _wrapped.StartOutput(f);
        }
    }

    public class StatusWrapper : ITProgress
    {
        private readonly Action<double, long?> _act;
        private readonly ITProgress _wrapped;

        public StatusWrapper(ITProgress wrapped, Action<double, long?> act) {
            _wrapped = wrapped;
            _act = act;
        }

        public long? Speed => _wrapped.Speed;
        public double Progress => _wrapped.Progress;

        public void Update(long? speed, double progress) {
            _wrapped.Update(speed, progress);
            _act(progress, speed);
        }
    }
}