// <copyright company="SIX Networks GmbH" file="PackageManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using MoreLinq;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;
using SN.withSIX.Sync.Core.Legacy;
using SN.withSIX.Sync.Core.Legacy.Status;
using SN.withSIX.Sync.Core.Packages.Internals;
using SN.withSIX.Sync.Core.Repositories;
using SN.withSIX.Sync.Core.Repositories.Internals;
using SN.withSIX.Sync.Core.Transfer;

namespace SN.withSIX.Sync.Core.Packages
{
    public class PackageManagerSettings
    {
        [Obsolete(
            "Should no longer be needed once we refactor all to install packages to a certain folder, and then use symlinks when needed"
            )]
        public IAbsoluteDirectoryPath GlobalWorkingPath { get; set; }
        public CheckoutType CheckoutType { get; set; } = CheckoutType.NormalCheckout;
    }

    public enum CheckoutType
    {
        NormalCheckout,
        CheckoutWithoutRemoval
    }

    public class PackageProgress
    {
        public ProgressLeaf PackageFetching { get; set; }
        public ProgressContainer Processing { get; set; }
        public ProgressLeaf Cleanup { get; set; }
    }

    public class PackageManager : IEnableLogging
    {
        readonly string _remote;
        public readonly Repository Repo;

        public PackageManager(Repository repo, IAbsoluteDirectoryPath workDir, bool createWhenNotExisting = false,
            string remote = null) {
            Contract.Requires<ArgumentNullException>(repo != null);
            Contract.Requires<ArgumentNullException>(workDir != null);
            WorkDir = workDir;
            Repo = repo;
            StatusRepo = new StatusRepo();
            Settings = new PackageManagerSettings();

            Repository.Factory.HandlePathRequirements(WorkDir, Repo);

            if (!WorkDir.Exists) {
                if (!createWhenNotExisting)
                    throw new Exception("Workdir doesnt exist");
                WorkDir.MakeSurePathExists();
            }

            if (!string.IsNullOrWhiteSpace(remote)) {
                var config =
                    Repository.DeserializeJson<RepositoryConfigDto>(
                        FetchString(Tools.Transfer.JoinUri(new Uri(remote), "config.json")));
                if (config.Uuid == Guid.Empty)
                    throw new Exception("Invalid remote, does not contain an UUID");
                Repo.AddRemote(config.Uuid, remote);
                Repo.Save();
            }

            Repository.Log("Opening repository at: {0}. Working directory at: {1}", Repo.RootPath, WorkDir);
            _remote = remote;
        }

        public static PackageProgress SetupSynqProgress(string title = "Network mods") {
            var packageFetching = new ProgressLeaf("Preparing");
            var networkModsProcessing = new ProgressContainer("Downloading", 9);
            var cleanup = new ProgressLeaf("Cleaning");

            return new PackageProgress {
                PackageFetching = packageFetching,
                Processing = networkModsProcessing,
                Cleanup = cleanup
            };
        }

        public PackageProgress Progress { get; set; } = SetupSynqProgress();

        public PackageManagerSettings Settings { get; }
        public StatusRepo StatusRepo { get; set; }
        public IAbsoluteDirectoryPath WorkDir { get; }

        [Obsolete("Workaround for classic callers")]
        public static async Task<PackageManager> Create(Repository repo, IAbsoluteDirectoryPath workDir,
            bool createWhenNotExisting = false,
            string remote = null) {
            var pm = new PackageManager(repo, workDir, createWhenNotExisting, remote);
            await repo.RefreshRemotes().ConfigureAwait(false);
            return pm;
        }

        void HandleRemotes(string[] remotes) {
            var config = GetConfigFromRemotes(remotes);
            Repo.AddRemote(config.Uuid, remotes);
            Repo.Save();
        }

        RepositoryConfigDto GetConfigFromRemotes(IEnumerable<string> remotes) {
            RepositoryConfigDto config = null;
            foreach (var r in remotes) {
                try {
                    config = TryGetConfigFromRemote(r);
                    break;
                } catch (Exception e) {
                    this.Logger().FormattedWarnException(e, "failure to retrieve config.json");
                }
            }
            if (config == null)
                throw new Exception("Could not get a valid config from any remote");
            return config;
        }

        static RepositoryConfigDto TryGetConfigFromRemote(string r) {
            var config = Repository.DeserializeJson<RepositoryConfigDto>(
                FetchString(Tools.Transfer.JoinUri(new Uri(r), "config.json")));
            if (config.Uuid == Guid.Empty)
                throw new Exception("Invalid remote, does not contain an UUID");
            return config;
        }

        static string FetchString(Uri uri) => SyncEvilGlobal.StringDownloader.Download(uri);

        public async Task UpdateRemotes() {
            await Repo.RefreshRemotes(_remote).ConfigureAwait(false);
            await Repo.UpdateRemotes().ConfigureAwait(false);
        }

        public async Task<List<Package>> Checkout(IReadOnlyCollection<string> packageNames,
            bool? useFullNameOverride = null) {
            StatusRepo.Reset(RepoStatus.CheckOut, packageNames.Count());
            var packages = new List<Package>();
            foreach (var p in packageNames)
                packages.Add(await CheckoutAsync(p, useFullNameOverride).ConfigureAwait(false));
            return packages;
        }

        async Task<Package> CheckoutAsync(string packageName, bool? useFullNameOverride = null) {
            var useFullName = Repo.Config.UseVersionedPackageFolders;

            if (useFullNameOverride.HasValue)
                useFullName = useFullNameOverride.Value;

            var depInfo = Repo.ResolvePackageName(packageName);
            if (depInfo == null)
                throw new Exception("Could not resolve package " + packageName);

            var resolvedPackageName = depInfo.GetFullName();

            IAbsoluteDirectoryPath dir;
            if (Repo.Config.OperationMode == RepositoryOperationMode.SinglePackage)
                dir = WorkDir;
            else {
                var name = useFullName ? depInfo.GetFullName() : depInfo.Name;
                var repoRoot = Repo.RootPath.ParentDirectoryPath;
                //dir = Path.Combine(repoRoot, name);
                var d = Tools.FileUtil.IsPathRootedIn(WorkDir, repoRoot, true) ? repoRoot : WorkDir;
                dir = d.GetChildDirectoryWithName(name);
            }

            Repository.Log("\nChecking out {0} into {1}, please be patient...", resolvedPackageName, dir);
            var package = Package.Factory.Open(Repo, dir, resolvedPackageName);
            package.StatusRepo = StatusRepo;
            await package.CheckoutAsync(null).ConfigureAwait(false);

            Repository.Log("\nSuccesfully checked out {0}", package.MetaData.GetFullName());

            return package;
        }

        public Task<Package[]> ProcessPackage(SpecificVersion package, bool? useFullNameOverride = null,
            bool noCheckout = false, bool skipWhenFileMatches = true)
            => ProcessPackage(package.ToDependency(), useFullNameOverride, noCheckout, skipWhenFileMatches);

        public Task<Package[]> ProcessPackage(Dependency package, bool? useFullNameOverride = null,
            bool noCheckout = false, bool skipWhenFileMatches = true)
            => ProcessPackages(new[] {package}, useFullNameOverride, noCheckout, skipWhenFileMatches);

        public Task<Package[]> ProcessPackages(IEnumerable<SpecificVersion> packageNames,
            bool? useFullNameOverride = null,
            bool noCheckout = false, bool skipWhenFileMatches = true)
            => ProcessPackages(packageNames.Select(x => x.ToDependency()), useFullNameOverride, noCheckout,
                skipWhenFileMatches);

        public async Task<Package[]> ProcessPackages(IEnumerable<Dependency> packageNames,
            bool? useFullNameOverride = null,
            bool noCheckout = false, bool skipWhenFileMatches = true) {
            if (Repo.Config.OperationMode == RepositoryOperationMode.SinglePackage)
                throw new Exception("Cannot process repository in SinglePackage mode");
            var useFullName = Repo.Config.UseVersionedPackageFolders;

            if (useFullNameOverride.HasValue)
                useFullName = useFullNameOverride.Value;

            // Add this package, then add it's dependencies, and so on
            // The list should be unique based on package (name + version + branch)
            // If there is a conflict, the process should be aborted. A conflict can arise when one package needs a version locked on X, and another on Y
            // First resolve all dependencies
            // So that we can determine conflicting dependencies, remove double dependencies, etc.
            // Let the package Download itself, by providing it with sources (remotes)
            var packages =
                (await GetDependencyTree(packageNames.ToArray(), noCheckout, useFullName).ConfigureAwait(false))
                    .OrderByDescending(x => x.MetaData.SizePacked).ToArray();

            StatusRepo.Reset(RepoStatus.Processing, packages.Length);

            if (Settings.GlobalWorkingPath != null) {
                var s = Settings.GlobalWorkingPath.ToString();
                foreach (var p in packages)
                    p.SetWorkingPath(s);
            }

            await ProcessModern(noCheckout, packages).ConfigureAwait(false);
            //await ProcessLegacy(noCheckout, skipWhenFileMatches, packages);

            await Repo.SaveAsync().ConfigureAwait(false);

            return packages.ToArray();
        }

        private async Task ProcessModern(bool noCheckout, Package[] packages) {
            if (noCheckout)
                throw new NotSupportedException();

            var doRemoval = Settings.CheckoutType != CheckoutType.CheckoutWithoutRemoval;
            var synqer = new Synqer();
            var allRemotes = packages.SelectMany(x => FindRemotesWithPackage(x.GetFullName())).Distinct().ToArray();
            await
                synqer.DownloadPackages(packages,
                    Repo.ObjectsPath.GetChildDirectoryWithName("temp"),
                    allRemotes, StatusRepo, doRemoval ? Progress.Cleanup : null, Progress.Processing)
                    .ConfigureAwait(false);
            // TODO: Only when changed?
            foreach (var p in packages)
                p.WriteTag();
        }

        private async Task ProcessLegacy(bool noCheckout, bool skipWhenFileMatches,
            IReadOnlyCollection<Package> packages) {
            await TrySyncObjects(noCheckout, skipWhenFileMatches, packages).ConfigureAwait(false);

            await
                CleanPackages(packages.Select(x => x.MetaData.ToSpecificVersion()).Distinct().ToArray(),
                    packages.Select(x => x.MetaData.Name).Distinct().ToArray()).ConfigureAwait(false);
        }

        public async Task<Package> DownloadPackage(string packageName, bool? useFullNameOverride = null) {
            var depInfo = ResolvePackageName(packageName);

            var useFullName = Repo.Config.UseVersionedPackageFolders;
            var name = depInfo.GetFullName();

            if (useFullNameOverride.HasValue)
                useFullName = useFullNameOverride.Value;

            await GetAndAddPackage(depInfo).ConfigureAwait(false);
            var package = Package.Factory.Open(Repo,
                Repo.Config.OperationMode == RepositoryOperationMode.Default
                    ? WorkDir.GetChildDirectoryWithName(useFullName ? name : depInfo.Name)
                    : WorkDir,
                name);

            await
                UpdateMultiple(package.GetNeededObjects(), new[] {package}, FindRemotesWithPackage(name).ToArray())
                    .ConfigureAwait(false);
            return package;
        }

        async Task<List<Package>> GetDependencyTree(IReadOnlyCollection<Dependency> dependencies, bool noCheckout,
            bool useFullName) {
            var list = new List<string>();
            var list2 = new List<string>();
            var packages = new List<Package>();

            await
                Progress.PackageFetching.Do(() => GetDependencyTreeInternal(dependencies, noCheckout, useFullName, list, list2, packages)).ConfigureAwait(false);
            return packages;
        }

        private async Task GetDependencyTreeInternal(IReadOnlyCollection<Dependency> dependencies, bool noCheckout, bool useFullName, List<string> list,
            List<string> list2, List<Package> packages) {
            StatusRepo.Reset(RepoStatus.Resolving, dependencies.Count);

            var specificVersions = dependencies.Select(x => ResolvePackageName(x.GetFullName())).ToArray();
            await FetchAllRequestedPackages(specificVersions).ConfigureAwait(false);

            var done = specificVersions.ToList();
            foreach (var dep in specificVersions) {
                Repo.AddPackage(dep.GetFullName());
                await
                    ResolveDependencies(list, list2, packages, dep, done, useFullName, noCheckout)
                        .ConfigureAwait(false);
            }

            foreach (var package in packages)
                package.StatusRepo = StatusRepo;

            if (packages.GroupBy(x => x.MetaData.Name.ToLower()).Any(x => x.Count() > 1))
                throw new InvalidOperationException("Somehow got duplicate packges: " +
                                                    string.Join(", ",
                                                        packages.GroupBy(x => x.MetaData.Name.ToLower())
                                                            .Where(x => x.Count() > 1)
                                                            .Select(x => x.Key)));
        }

        private Task FetchAllRequestedPackages(IReadOnlyCollection<SpecificVersion> specificVersions) {
            var i = 0;
            var totalCount = specificVersions.Count;
            var lObject = new object();
            var allRemotes = specificVersions.SelectMany(x => FindRemotesWithPackage(x.GetFullName())).Distinct().ToArray();
            return
                SyncEvilGlobal.DownloadHelper.DownloadFilesAsync(allRemotes, StatusRepo,
                    specificVersions.ToDictionary(x => new FileFetchInfo("packages/" + x.GetFullName() + ".json"),
                        x =>
                            (ITransferStatus)
                                new Status(x.GetFullName(), StatusRepo) {
                                    RealObject = "packages/" + x + ".json",
                                    OnComplete =
                                        () => {
                                            lock (lObject)
                                                Progress.PackageFetching.Update(null, (++i).ToProgress(totalCount));
                                            return TaskExt.Default;
                                        }
                                }),
                    Repo.RootPath);
        }

        async Task TrySyncObjects(bool noCheckout, bool skipWhenFileMatches, IReadOnlyCollection<Package> packages) {
            var syncedPackages = new List<Package>();
            try {
                await
                    ProcessSynqObjects(noCheckout, skipWhenFileMatches, packages, syncedPackages).ConfigureAwait(false);
            } finally {
                Repo.AddPackage(syncedPackages.Select(x => x.MetaData.GetFullName()).ToArray());
            }
        }

        async Task ProcessSynqObjects(bool noCheckout, bool skipWhenFileMatches, IReadOnlyCollection<Package> packages,
            ICollection<Package> syncedPackages) {
            var allRemotes = new List<Uri>();

            var objectsToFetch = GetObjectsToFetch(skipWhenFileMatches, packages, allRemotes);
            await
                UpdateMultiple(objectsToFetch, packages,
                    allRemotes.Distinct().ToArray()).ConfigureAwait(false);

            await
                ProcessCheck(noCheckout, skipWhenFileMatches, packages, syncedPackages, objectsToFetch)
                    .ConfigureAwait(false);
        }

        private Package.ObjectMap[] GetObjectsToFetch(bool skipWhenFileMatches, IReadOnlyCollection<Package> packages,
            List<Uri> allRemotes) {
            var objects = new List<Package.ObjectMap>();
            var j = 0;
            foreach (var package in packages) {
                var name = package.MetaData.GetFullName();
                var remotes = FindRemotesWithPackage(name).ToArray();
                Console.WriteLine(string.Empty);
                if (!remotes.Any())
                    throw new NoSourceFoundException("No source found with " + name);
                allRemotes.AddRange(remotes);
                Repository.Log("Processing package: {0}", name);
                objects.AddRange(package.GetNeededObjects(skipWhenFileMatches));
            }

            return objects.DistinctBy(x => x.FO.Checksum).ToArray();
        }

        private async Task ProcessCheck(bool noCheckout, bool skipWhenFileMatches, IReadOnlyCollection<Package> packages,
            ICollection<Package> syncedPackages, Package.ObjectMap[] objectsToFetch) {
            StatusRepo.Reset(RepoStatus.CheckOut, packages.Count);
            var i = 0;
            syncedPackages.AddRange(packages);
            var components = packages.Select(x => new ProgressLeaf(x.MetaData.Name)).ToArray();
            if (!noCheckout) {
                foreach (var package in packages) {
                    var p = components[i++];
                    await p.Do(() => ProcessPackageInternal(package, p)).ConfigureAwait(false);
                }
            }

            if (!noCheckout && skipWhenFileMatches) {
                Repo.DeleteObject(objectsToFetch.Select(x => new ObjectInfo(x.FO.Checksum, x.FO.Checksum)));
                // nasty using checksum for packed checksum or ? not needed here anyway??
            }
        }

        private async Task ProcessPackageInternal(Package package, ProgressLeaf p) {
            Repository.Log("\nChecking out {0} into {1}, please be patient...", package.MetaData.GetFullName(),
                package.WorkingPath);
            switch (Settings.CheckoutType) {
            case CheckoutType.NormalCheckout:
                await package.CheckoutAsync(p).ConfigureAwait(false);
                break;
            case CheckoutType.CheckoutWithoutRemoval:
                await package.CheckoutWithoutRemovalAsync(p).ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        async Task<string[]> UpdateMultiple(IReadOnlyCollection<Package.ObjectMap> objects,
            IReadOnlyCollection<Package> packages,
            IReadOnlyCollection<Uri> remotes) {
            if (!objects.Any()) {
                Repository.Log("No remote objects to resolve");
                return new string[0];
            }
            Repository.Log("Resolving {0} remote objects for {1} packages from {2} remotes, please be patient..",
                objects.Count, packages.Count, remotes.Count);

            var doneObjects = new ConcurrentBag<FileObjectMapping>();

            var relObjects = objects.OrderByDescending(x => Tools.FileUtil.SizePrediction(x.FO.FilePath))
                .Select(x => new FileFetchInfo(Repo.GetObjectSubPath(x.FO), x.FO.FilePath) {
                    ExistingPath = x.ExistingObject != null ? Repo.GetObjectSubPath(x.ExistingObject) : null,
                    OnComplete = () => doneObjects.Add(x.FO)
                })
                .ToArray();

            StatusRepo.Reset(RepoStatus.Downloading, objects.Count);
            StatusRepo.ProcessSize(GetExistingObjects(objects.Select(x => x.FO), packages), Repo.ObjectsPath,
                GetPackedSize(packages));
            try {
                await
                    Package.DownloadObjects(remotes, StatusRepo, relObjects, Repo.ObjectsPath).ConfigureAwait(false);
            } finally {
                Repo.ReAddObject(doneObjects.Select(x => x.Checksum).ToArray());
            }

            return relObjects.Select(x => x.FilePath).ToArray();
        }

        // Not accurate when there are duplicate objects between packages as they are de-duplicated. But not too important.
        static long GetPackedSize(IEnumerable<Package> packages) => packages.Sum(x => x.MetaData.SizePacked);

        IEnumerable<string> GetExistingObjects(IEnumerable<FileObjectMapping> objects, IEnumerable<Package> packages)
            => packages.SelectMany(x => x.GetMetaDataFilesOrderedBySize().Select(y => y.Checksum))
                .Except(objects.Select(x => x.Checksum))
                .Select(x => Repo.GetObjectSubPath(x));

        Task GetPackage(SpecificVersion package) {
            var packageName = package.GetFullName();
            var remotes = FindRemotesWithPackage(package.Name).ToArray();
            Repository.Log("\nFetching package: {0} ({1})", package.Name, packageName);
            if (!remotes.Any())
                throw new NoSourceFoundException("No source found with " + package.Name);

            return Repo.DownloadPackage(packageName, remotes, StatusRepo.CancelToken);
        }

        public async Task GetAndAddPackage(SpecificVersion package) {
            //Somewhere here we will need to specify what we will be doing with the package.
            await GetPackage(package).ConfigureAwait(false);
            Repo.AddPackage(package.GetFullName());
        }

        SpecificVersion TryResolvePackageName(string packageName) {
            var depInfo = new Dependency(packageName);
            //var remotes = FindRemotesWithPackage(packageName);
            return Repo.Remotes.Select(x => x.Index.GetPackage(depInfo))
                .Where(x => x != null)
                .OrderBy(x => x)
                .LastOrDefault();
        }

        SpecificVersion ResolvePackageName(string packageName) {
            var resolvePackageName = TryResolvePackageName(packageName);
            if (resolvePackageName == null)
                    throw new NoSourceFoundException("Could not resolve package " + packageName);
            return resolvePackageName;
        }

        IEnumerable<Uri> FindRemotesWithPackage(string packageName)
            => FindRemotesWithPackage(new Dependency(packageName));

        IEnumerable<Uri> FindRemotesWithPackage(Dependency package)
            => Repo.Remotes.Where(x => x.Index.HasPackage(package))
                .SelectMany(x => x.GetRemotes()).Distinct();

        async Task ResolveDependencies(List<string> list, List<string> list2, List<Package> packages,
            SpecificVersion depInfo, List<SpecificVersion> done, bool useFullName = false, bool noCheckout = false) {
            if (!noCheckout && list.Contains(depInfo.Name.ToLower())) {
                Repository.Log("Conflicting package, not resolving {0}", depInfo);
                return;
            }
            var name = depInfo.GetFullName();
            if (list2.Contains(name)) {
                Repository.Log("Duplicate package, skipping {0}", name);
                return;
            }
            list2.Add(name);

            if (!done.Contains(depInfo)) {
                await GetAndAddPackage(depInfo).ConfigureAwait(false);
                done.Add(depInfo);
            }

            var package = Package.Factory.Open(Repo,
                WorkDir.GetChildDirectoryWithName(useFullName ? name : depInfo.Name), name);
            list.Add(depInfo.Name);
            packages.Add(package);

            // TODO: Higher level policy can be overwritten by dependencies (e.g specified dependency contraints). We dont want this.
            foreach (var dep in package.MetaData.GetDependencies())
                await ResolveDependencies(list, list2, packages, ResolvePackageName(dep.GetFullName()), done, useFullName, noCheckout).ConfigureAwait(false);

            OrderPackageLast(list, packages, package);
        }

        static void OrderPackageLast(ICollection<string> list, ICollection<Package> packages, Package package) {
            var name = package.MetaData.Name.ToLower();
            list.Remove(name);
            list.Add(name);
            packages.Remove(package);
            packages.Add(package);
        }

        public Task<IEnumerable<string>> List(bool remote = false) => Repo.ListPackages(remote);

        public Task<IEnumerable<string>> List(string remote) {
            if (remote == null)
                return List();
            return remote == string.Empty ? List(true) : Repo.ListPackages(remote);
        }

        public void DeletePackages(IEnumerable<SpecificVersion> packages, bool inclWorkFiles = false,
            bool inclDependencies = false) {
            Repo.DeletePackage(packages, inclWorkFiles, inclDependencies);
            Repo.RemoveObsoleteObjects();
            Repo.Save();
        }

        public void DeletePackagesThatExist(IEnumerable<SpecificVersion> packages, bool inclWorkFiles = false,
            bool inclDependencies = false) {
            Repo.DeletePackage(packages.Where(x => Repo.HasPackage(x)), inclWorkFiles, inclDependencies);
            Repo.RemoveObsoleteObjects();
            Repo.Save();
        }

        public void DeletePackages(IEnumerable<string> packages, bool inclWorkFiles = false,
            bool inclDependencies = false) {
            DeletePackages(packages.Select(x => new SpecificVersion(x)), inclWorkFiles, inclDependencies);
        }

        public void DeletePackage(SpecificVersion package, bool inclWorkFiles = false,
            bool inclDependencies = false) {
            Repo.DeletePackage(package, inclWorkFiles, inclDependencies);
            Repo.RemoveObsoleteObjects();
            Repo.Save();
        }

        public void DeletePackageIfExists(Dependency package, bool inclWorkFiles = false,
            bool inclDependencies = false) {
            if (Repo.HasPackage(package))
                DeletePackage(Repo.Index.GetPackage(package), inclWorkFiles, inclDependencies);
        }

        public void DeletePackageIfExists(SpecificVersion package, bool inclWorkFiles = false,
            bool inclDependencies = false) {
            if (Repo.HasPackage(package))
                DeletePackage(package, inclWorkFiles, inclDependencies);
        }

        public void DeleteBundle(IEnumerable<string> collections, bool inclPackages = false,
            bool inclDependencies = false, bool inclPackageWorkFiles = false) {
            Repo.DeleteBundle(collections);
        }

        public IEnumerable<PackageMetaData> GetPackages(bool useFullName = false) => Repo.GetPackagesListAsVersions()
            .Select(GetMetaData);

        public Dictionary<string, SpecificVersionInfo[]> GetPackagesAsVersions(bool remote = false) {
            var packages = remote
                ? Repo.Remotes.SelectMany(x => x.Index.GetPackagesListAsVersions()).Distinct().OrderBy(x => x).ToArray()
                : Repo.GetPackagesListAsVersions();

            var dic = new Dictionary<string, List<SpecificVersionInfo>>();
            foreach (var i in packages) {
                if (!dic.ContainsKey(i.Name))
                    dic[i.Name] = new List<SpecificVersionInfo>();
                dic[i.Name].Add(i.VersionInfo);
            }

            return dic.ToDictionary(x => x.Key, x => x.Value.Select(y => y).Reverse().ToArray());
        }

        public PackageMetaData GetMetaData(SpecificVersion arg) => Package.Load(GetPackageMetadataPath(arg));

        IAbsoluteFilePath GetPackageMetadataPath(SpecificVersion arg)
            => Repo.PackagesPath.GetChildFileWithName(arg.GetFullName() + Repository.PackageFormat);

        public Task<IReadOnlyCollection<SpecificVersion>> CleanPackages(
            IReadOnlyCollection<SpecificVersion> keepVersions, params string[] packages) {
            StatusRepo.Reset(RepoStatus.Cleaning, packages.Length);
            return Repo.CleanPackageAsync(packages, keepVersions);
        }

        public Task<IReadOnlyCollection<SpecificVersion>> CleanPackages(int limit,
            IReadOnlyCollection<SpecificVersion> keepVersions, params string[] packages) {
            StatusRepo.Reset(RepoStatus.Cleaning, packages.Length);
            return Repo.CleanPackageAsync(packages, keepVersions, limit);
        }
    }
}