// <copyright company="SIX Networks GmbH" file="ContentDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SteamKit2;
using withSIX.Api.Models.Exceptions;

namespace SN.withSIX.Steam.Api.SteamKit.DepotDownloader
{
    public static class FileExtensions
    {
        public static void MakeSureExists(this IAbsoluteDirectoryPath path) {
            if (!path.Exists)
                Directory.CreateDirectory(path.ToString());
        }
    }

    public class ContentDownloader : IDisposable
    {
        public const uint INVALID_APP_ID = uint.MaxValue;
        public const uint INVALID_DEPOT_ID = uint.MaxValue;
        public const ulong INVALID_MANIFEST_ID = ulong.MaxValue;

        private const string CONFIG_DIR = ".DepotDownloader";
        private static readonly string STAGING_DIR = Path.Combine(CONFIG_DIR, "staging");
        private readonly DownloadConfig _config;
        private CDNClientPool _cdnPool;
        private Steam3Session _steam3;
        private Steam3Session.Credentials _steam3Credentials;

        public ContentDownloader(DownloadConfig config) {
            _config = config;
        }

        public static Action<string> Log { get; set; } = Console.WriteLine;
        public static Func<string, Task<string>> GetDetails { get; set; } = x => Task.FromResult(Console.ReadLine());

        public void Dispose() {
            _steam3?.Disconnect();
            _cdnPool?.Dispose();
        }

        void CreateDirectories() {
            _config.InstallDirectory.MakeSureExists();
            _config.InstallDirectory.GetChildDirectoryWithName(CONFIG_DIR).MakeSureExists();
            _config.InstallDirectory.GetChildDirectoryWithName(STAGING_DIR).MakeSureExists();
        }

        bool TestIsFileIncluded(string filename) => !_config.UsingFileList ||
                                                    _config.FilesToDownload.Any(
                                                        fileListEntry =>
                                                            fileListEntry.Equals(filename,
                                                                StringComparison.OrdinalIgnoreCase)) ||
                                                    _config.FilesToDownloadRegex.Select(rgx => rgx.Match(filename))
                                                        .Any(m => m.Success);

        bool AccountHasAccess(uint depotId) {
            if ((_steam3.steamUser.SteamID == null) ||
                ((_steam3.Licenses == null) && (_steam3.steamUser.SteamID.AccountType != EAccountType.AnonUser)))
                return false;

            var licenseQuery = _steam3.steamUser.SteamID.AccountType == EAccountType.AnonUser
                ? new List<uint> {17906}
                : _steam3.Licenses.Select(x => x.PackageID);

            _steam3.RequestPackageInfo(licenseQuery);

            foreach (var license in licenseQuery) {
                SteamApps.PICSProductInfoCallback.PICSProductInfo package;
                if (!_steam3.PackageInfo.TryGetValue(license, out package) || (package == null))
                    continue;
                if (package.KeyValues["appids"].Children.Any(child => child.AsInteger() == depotId))
                    return true;
                if (package.KeyValues["depotids"].Children.Any(child => child.AsInteger() == depotId))
                    return true;
            }

            return false;
        }

        internal KeyValue GetSteam3AppSection(uint appId, EAppInfoSection section) {
            if (_steam3.AppInfo == null)
                return null;

            SteamApps.PICSProductInfoCallback.PICSProductInfo app;
            if (!_steam3.AppInfo.TryGetValue(appId, out app) || (app == null))
                return null;

            var appinfo = app.KeyValues;
            string section_key;

            switch (section) {
            case EAppInfoSection.Common:
                section_key = "common";
                break;
            case EAppInfoSection.Extended:
                section_key = "extended";
                break;
            case EAppInfoSection.Config:
                section_key = "config";
                break;
            case EAppInfoSection.Depots:
                section_key = "depots";
                break;
            default:
                throw new NotImplementedException();
            }

            return appinfo.Children.FirstOrDefault(c => c.Name == section_key);
        }

        uint GetSteam3AppBuildNumber(uint appId, string branch) {
            if (appId == INVALID_APP_ID)
                return 0;


            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);
            var branches = depots["branches"];
            var node = branches[branch];

            if (node == KeyValue.Invalid)
                return 0;

            var buildid = node["buildid"];

            return buildid == KeyValue.Invalid ? 0 : uint.Parse(buildid.Value);
        }

        ulong GetSteam3DepotManifest(uint depotId, uint appId, string branch) {
            if (_config.ManifestId != INVALID_MANIFEST_ID)
                return _config.ManifestId;

            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);
            var depotChild = depots[depotId.ToString()];

            if (depotChild == KeyValue.Invalid)
                return INVALID_MANIFEST_ID;

            if (depotChild["depotfromapp"] != KeyValue.Invalid) {
                var otherAppId = (uint) depotChild["depotfromapp"].AsInteger();
                if (otherAppId == appId) {
                    // This shouldn't ever happen, but ya never know with Valve. Don't infinite loop.
                    Log($"App {appId}, Depot {depotId} has depotfromapp of {otherAppId}!");
                    return INVALID_MANIFEST_ID;
                }

                _steam3.RequestAppInfo(otherAppId);

                return GetSteam3DepotManifest(depotId, otherAppId, branch);
            }

            var manifests = depotChild["manifests"];
            var manifests_encrypted = depotChild["encryptedmanifests"];

            if ((manifests.Children.Count == 0) && (manifests_encrypted.Children.Count == 0))
                return INVALID_MANIFEST_ID;

            var node = manifests[branch];

            if ((branch != "Public") && (node == KeyValue.Invalid)) {
                var node_encrypted = manifests_encrypted[branch];
                if (node_encrypted == KeyValue.Invalid)
                    return INVALID_MANIFEST_ID;

                var password = _config.BetaPassword;
                if (password == null) {
                    Log($"Please enter the password for branch {branch}: ");
                    _config.BetaPassword = password = GetDetails("betapassword").Result;
                }

                var input = Util.DecodeHexString(node_encrypted["encrypted_gid"].Value);
                var manifest_bytes = CryptoHelper.VerifyAndDecryptPassword(input, password);

                if (manifest_bytes != null)
                    return BitConverter.ToUInt64(manifest_bytes, 0);
                Log($"Password was invalid for branch {branch}");
                return INVALID_MANIFEST_ID;
            }

            return node.Value == null ? INVALID_MANIFEST_ID : ulong.Parse(node.Value);
        }

        string GetAppOrDepotName(uint depotId, uint appId) {
            if (depotId == INVALID_DEPOT_ID) {
                var info = GetSteam3AppSection(appId, EAppInfoSection.Common);
                return info == null ? string.Empty : info["name"].AsString();
            }
            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);

            var depotChild = depots?[depotId.ToString()];

            return depotChild == null ? string.Empty : depotChild["name"].AsString();
        }

        public bool InitializeSteam3(string username, string password) {
            _steam3 = new Steam3Session(
                new SteamUser.LogOnDetails {
                    Username = username,
                    Password = password,
                    CellID = _config.CellID
                });

            _steam3Credentials = _steam3.WaitForCredentials();

            if (!_steam3Credentials.IsValid) {
                Log("Unable to get steam3 credentials.");
                throw new UnauthorizedException("Unable to get steam3 credentials");
                return false;
            }

            _cdnPool = new CDNClientPool(_steam3);
            return true;
        }

        public void DownloadApp(uint appId, uint depotId, string branch, bool forceDepot = false) {
            _steam3.RequestAppInfo(appId);

            if (!AccountHasAccess(appId)) {
                var contentName = GetAppOrDepotName(INVALID_DEPOT_ID, appId);
                throw new ForbiddenException($"App {appId} ({contentName}) is not available from this account.");
            }

            var depotIDs = new List<uint>();
            var depots = GetSteam3AppSection(appId, EAppInfoSection.Depots);


            if (forceDepot)
                depotIDs.Add(depotId);
            else {
                if (depots != null) {
                    foreach (var depotSection in depots.Children) {
                        var id = INVALID_DEPOT_ID;
                        if (depotSection.Children.Count == 0)
                            continue;

                        if (!uint.TryParse(depotSection.Name, out id))
                            continue;

                        if ((depotId != INVALID_DEPOT_ID) && (id != depotId))
                            continue;

                        if (!_config.DownloadAllPlatforms) {
                            var depotConfig = depotSection["config"];
                            if ((depotConfig != KeyValue.Invalid) && (depotConfig["oslist"] != KeyValue.Invalid) &&
                                !string.IsNullOrWhiteSpace(depotConfig["oslist"].Value)) {
                                var oslist = depotConfig["oslist"].Value.Split(',');
                                if (Array.IndexOf(oslist, Util.GetSteamOS()) == -1)
                                    continue;
                            }
                        }

                        depotIDs.Add(id);
                    }
                }
                if (!depotIDs.Any()) {
                    if (depotId == INVALID_DEPOT_ID)
                        throw new NotFoundException($"Couldn't find any depots to download for app {appId}");
                    throw new NotFoundException(
                        $"Depot {depotId} not listed for app {appId} {(!_config.DownloadAllPlatforms ? "or not available on this platform" : "")}");
                }
            }

            var infos =
                depotIDs.Select(depot => GetDepotInfo(depot, appId, branch)).Where(info => info != null).ToList();
            if (!infos.Any())
                throw new NotFoundException($"No usable depots found");
            DownloadSteam3(infos);
        }

        DepotDownloadInfo GetDepotInfo(uint depotId, uint appId, string branch) {
            if (appId != INVALID_APP_ID)
                _steam3.RequestAppInfo(appId);

            var contentName = GetAppOrDepotName(depotId, appId);

            if (!AccountHasAccess(depotId)) {
                Log($"Depot {depotId} ({contentName}) is not available from this account.");
                return null;
            }

            _steam3.RequestAppTicket(depotId);

            var manifestID = GetSteam3DepotManifest(depotId, appId, branch);
            if ((manifestID == INVALID_MANIFEST_ID) && (branch != "public")) {
                Log($"Warning: Depot {depotId} does not have branch named \"{branch}\". Trying public branch.");
                branch = "public";
                manifestID = GetSteam3DepotManifest(depotId, appId, branch);
            }

            if (manifestID == INVALID_MANIFEST_ID) {
                Log($"Depot {depotId} ({contentName}) missing public subsection or manifest section.");
                return null;
            }

            var uVersion = GetSteam3AppBuildNumber(appId, branch);

            if (_config.InstallDirectory == null) {
                var depotPath = Path.Combine(Directory.GetCurrentDirectory(), "depots")
                    .ToAbsoluteDirectoryPath()
                    .GetChildDirectoryWithName(depotId.ToString());
                depotPath.MakeSureExists();
                _config.InstallDirectory = depotPath.GetChildDirectoryWithName(uVersion.ToString());
            }
            CreateDirectories();

            _steam3.RequestDepotKey(depotId, appId);
            if (!_steam3.DepotKeys.ContainsKey(depotId)) {
                Log($"No valid depot key for {depotId}, unable to download.");
                return null;
            }

            var depotKey = _steam3.DepotKeys[depotId];
            return new DepotDownloadInfo(depotId, manifestID, _config.InstallDirectory, contentName) {
                DepotKey = depotKey
            };
        }

        private void DownloadSteam3(IReadOnlyCollection<DepotDownloadInfo> depots) {
            ulong totalBytesCompressed = 0;
            ulong totalBytesUncompressed = 0;

            foreach (var depot in depots) {
                ulong depotBytesCompressed = 0;
                ulong depotBytesUncompressed = 0;

                Log($"Downloading depot {depot.ID} - {depot.ContentName}");

                ProtoManifest oldProtoManifest = null;
                ProtoManifest newProtoManifest = null;
                var configDir = depot.InstallDir.GetChildDirectoryWithName(CONFIG_DIR);

                var lastManifestId = INVALID_MANIFEST_ID;
                ConfigStore.TheConfig.LastManifests.TryGetValue(depot.ID, out lastManifestId);

                // In case we have an early exit, this will force equiv of verifyall next run.
                ConfigStore.TheConfig.LastManifests[depot.ID] = INVALID_MANIFEST_ID;
                ConfigStore.Save();

                if (lastManifestId != INVALID_MANIFEST_ID) {
                    var oldManifestFileName = configDir.GetChildFileWithName($"{lastManifestId}.bin");
                    if (oldManifestFileName.Exists)
                        oldProtoManifest = ProtoManifest.LoadFromFile(oldManifestFileName);
                }

                if ((lastManifestId == depot.ManifestId) && (oldProtoManifest != null)) {
                    newProtoManifest = oldProtoManifest;
                    Log($"Already have manifest {depot.ManifestId} for depot {depot.ID}.");
                } else {
                    var newManifestFileName = configDir.GetChildFileWithName($"{depot.ManifestId}.bin");
                    if (newManifestFileName != null)
                        newProtoManifest = ProtoManifest.LoadFromFile(newManifestFileName);

                    if (newProtoManifest != null)
                        Log($"Already have manifest {depot.ManifestId} for depot {depot.ID}.");
                    else {
                        Log("Downloading depot manifest...");

                        DepotManifest depotManifest = null;

                        while (depotManifest == null) {
                            CDNClient client = null;
                            try {
                                client = _cdnPool.GetConnectionForDepot(depot.ID, depot.DepotKey,
                                    CancellationToken.None);

                                depotManifest = client.DownloadManifest(depot.ID, depot.ManifestId);

                                _cdnPool.ReturnConnection(client);
                            } catch (WebException e) {
                                _cdnPool.ReturnBrokenConnection(client);

                                if (e.Status == WebExceptionStatus.ProtocolError) {
                                    var response = (HttpWebResponse) e.Response;
                                    if ((response.StatusCode == HttpStatusCode.Unauthorized) ||
                                        (response.StatusCode == HttpStatusCode.Forbidden)) {
                                        Log(
                                            $"Encountered 401 for depot manifest {depot.ID} {depot.ManifestId}. Aborting.");
                                        break;
                                    }
                                    Log(
                                        $"Encountered error downloading depot manifest {depot.ID} {depot.ManifestId}: {response.StatusCode}");
                                } else {
                                    Log(
                                        $"Encountered error downloading manifest for depot {depot.ID} {depot.ManifestId}: {e.Status}");
                                }
                            } catch (Exception e) {
                                _cdnPool.ReturnBrokenConnection(client);
                                Log(
                                    $"Encountered error downloading manifest for depot {depot.ID} {depot.ManifestId}: {e.Message}");
                            }
                        }

                        if (depotManifest == null) {
                            Log($"\nUnable to download manifest {depot.ManifestId} for depot {depot.ID}");
                            return;
                        }

                        newProtoManifest = new ProtoManifest(depotManifest, depot.ManifestId);
                        newProtoManifest.SaveToFile(newManifestFileName);

                        Log(" Done!");
                    }
                }

                newProtoManifest.Files.Sort((x, y) => x.FileName.CompareTo(y.FileName));

                if (_config.DownloadManifestOnly) {
                    var manifestBuilder = new StringBuilder();
                    var txtManifest = depot.InstallDir.GetChildFileWithName($"manifest_{depot.ID}.txt");

                    foreach (
                        var file in
                        newProtoManifest.Files.Where(file => !file.Flags.HasFlag(EDepotFileFlag.Directory)))
                        manifestBuilder.Append($"{file.FileName}\n");

                    File.WriteAllText(txtManifest.ToString(), manifestBuilder.ToString());
                    continue;
                }

                ulong completeDownloadSize = 0;
                ulong sizeDownloaded = 0;
                var lastTime = Tools.Generic.GetCurrentUtcDateTime;
                ulong lastBytes = 0;
                var stagingDir = depot.InstallDir.GetChildDirectoryWithName(STAGING_DIR);

                var filesAfterExclusions =
                    newProtoManifest.Files.AsParallel().Where(f => TestIsFileIncluded(f.FileName)).ToList();

                // Pre-process
                filesAfterExclusions.ForEach(file => {
                    if (file.Flags.HasFlag(EDepotFileFlag.Directory)) {
                        var fileFinalPath = depot.InstallDir.GetChildDirectoryWithName(file.FileName);
                        var fileStagingPath = stagingDir.GetChildDirectoryWithName(file.FileName);
                        fileFinalPath.MakeSureExists();
                        fileStagingPath.MakeSureExists();
                    } else {
                        var fileFinalPath = depot.InstallDir.GetChildFileWithName(file.FileName);
                        var fileStagingPath = stagingDir.GetChildFileWithName(file.FileName);
                        // Some manifests don't explicitly include all necessary directories
                        fileFinalPath.ParentDirectoryPath.MakeSureExists();
                        fileStagingPath.ParentDirectoryPath.MakeSureExists();

                        completeDownloadSize += file.TotalSize;
                    }
                });

                filesAfterExclusions.Where(f => !f.Flags.HasFlag(EDepotFileFlag.Directory))
                    .AsParallel().WithCancellation(_config.CancelToken).WithDegreeOfParallelism(_config.MaxDownloads)
                    .ForAll(file => {
                        var fileFinalPath = depot.InstallDir.GetChildFileWithName(file.FileName);
                        var fileStagingPath = stagingDir.GetChildFileWithName(file.FileName);

                        // This may still exist if the previous run exited before cleanup
                        if (fileStagingPath.Exists)
                            fileStagingPath.FileInfo.Delete();

                        List<ProtoManifest.ChunkData> neededChunks;
                        FileStream fs = null;
                        if (!fileFinalPath.Exists) {
                            // create new file. need all chunks
                            fs = File.Create(fileFinalPath.ToString());
                            fs.SetLength((long) file.TotalSize);
                            neededChunks = new List<ProtoManifest.ChunkData>(file.Chunks);
                        } else {
                            // open existing
                            ProtoManifest.FileData oldManifestFile = null;
                            if (oldProtoManifest != null) {
                                oldManifestFile =
                                    oldProtoManifest.Files.SingleOrDefault(f => f.FileName == file.FileName);
                            }

                            if (oldManifestFile != null) {
                                neededChunks = new List<ProtoManifest.ChunkData>();

                                if (_config.VerifyAll || !oldManifestFile.FileHash.SequenceEqual(file.FileHash)) {
                                    // we have a version of this file, but it doesn't fully match what we want

                                    var matchingChunks = new List<ChunkMatch>();

                                    foreach (var chunk in file.Chunks) {
                                        var oldChunk =
                                            oldManifestFile.Chunks.FirstOrDefault(
                                                c => c.ChunkID.SequenceEqual(chunk.ChunkID));
                                        if (oldChunk != null)
                                            matchingChunks.Add(new ChunkMatch(oldChunk, chunk));
                                        else
                                            neededChunks.Add(chunk);
                                    }

                                    File.Move(fileFinalPath.ToString(), fileStagingPath.ToString());

                                    fs = new FileStream(fileFinalPath.ToString(), FileMode.Create);
                                    fs.SetLength((long) file.TotalSize);

                                    using (var fsOld = fileStagingPath.FileInfo.Open(FileMode.Open)) {
                                        foreach (var match in matchingChunks) {
                                            fsOld.Seek((long) match.OldChunk.Offset, SeekOrigin.Begin);

                                            var tmp = new byte[match.OldChunk.UncompressedLength];
                                            fsOld.Read(tmp, 0, tmp.Length);

                                            var adler = Util.AdlerHash(tmp);
                                            if (!adler.SequenceEqual(match.OldChunk.Checksum))
                                                neededChunks.Add(match.NewChunk);
                                            else {
                                                fs.Seek((long) match.NewChunk.Offset, SeekOrigin.Begin);
                                                fs.Write(tmp, 0, tmp.Length);
                                            }
                                        }
                                    }

                                    fileStagingPath.FileInfo.Delete();
                                }
                            } else {
                                // No old manifest or file not in old manifest. We must validate.

                                var fi = fileFinalPath.FileInfo;
                                fs = fi.Open(FileMode.Open);
                                if ((ulong) fi.Length != file.TotalSize)
                                    fs.SetLength((long) file.TotalSize);

                                neededChunks = Util.ValidateSteam3FileChecksums(fs,
                                    file.Chunks.OrderBy(x => x.Offset).ToArray());
                            }

                            if (!neededChunks.Any()) {
                                sizeDownloaded += file.TotalSize;
                                var progress = CalculateProgress(sizeDownloaded, completeDownloadSize);
                                var now = Tools.Generic.GetCurrentUtcDateTime;
                                long? speed = null;
                                if (lastBytes != 0) {
                                    var timeSpan = now - lastTime;
                                    var bytesChange = sizeDownloaded - lastBytes;

                                    if (timeSpan.TotalMilliseconds > 0)
                                        speed = (long) (bytesChange/(timeSpan.TotalMilliseconds/1000.0));
                                }
                                lastBytes = sizeDownloaded;
                                lastTime = now;
                                _config.Progress(speed, progress);
                                Log($"{progress,6:#00.00}% {speed}b/s {fileFinalPath}");
                                fs?.Close();
                                return;
                            }
                            sizeDownloaded += file.TotalSize -
                                              (ulong) neededChunks.Select(x => (long) x.UncompressedLength).Sum();
                        }

                        foreach (var chunk in neededChunks) {
                            if (_config.CancelToken.IsCancellationRequested)
                                break;

                            var chunkID = Util.EncodeHexString(chunk.ChunkID);
                            CDNClient.DepotChunk chunkData = null;

                            while (!_config.CancelToken.IsCancellationRequested) {
                                CDNClient client;
                                try {
                                    client = _cdnPool.GetConnectionForDepot(depot.ID, depot.DepotKey,
                                        _config.CancelToken);
                                } catch (OperationCanceledException) {
                                    break;
                                }

                                var data = new DepotManifest.ChunkData {
                                    ChunkID = chunk.ChunkID,
                                    Checksum = chunk.Checksum,
                                    Offset = chunk.Offset,
                                    CompressedLength = chunk.CompressedLength,
                                    UncompressedLength = chunk.UncompressedLength
                                };

                                try {
                                    chunkData = client.DownloadDepotChunk(depot.ID, data);
                                    _cdnPool.ReturnConnection(client);
                                    break;
                                } catch (WebException e) {
                                    _cdnPool.ReturnBrokenConnection(client);

                                    if (e.Status == WebExceptionStatus.ProtocolError) {
                                        var response = (HttpWebResponse) e.Response;
                                        if ((response.StatusCode == HttpStatusCode.Unauthorized) ||
                                            (response.StatusCode == HttpStatusCode.Forbidden)) {
                                            Log($"Encountered 401 for chunk {chunkID}. Aborting.");
                                            throw new OperationCanceledException(
                                                $"Encountered 401 for chunk {chunkID}. Aborting.");
                                        }
                                        Log($"Encountered error downloading chunk {chunkID}: {response.StatusCode}");
                                    } else
                                        Log($"Encountered error downloading chunk {chunkID}: {e.Status}");
                                } catch (Exception e) {
                                    _cdnPool.ReturnBrokenConnection(client);
                                    Log($"Encountered unexpected error downloading chunk {chunkID}: {e.Message}");
                                }
                            }

                            if (chunkData == null) {
                                Log(
                                    $"Failed to find any server with chunk {chunkID} for depot {depot.ID}. Aborting.");
                                return;
                            }

                            totalBytesCompressed += chunk.CompressedLength;
                            depotBytesCompressed += chunk.CompressedLength;
                            totalBytesUncompressed += chunk.UncompressedLength;
                            depotBytesUncompressed += chunk.UncompressedLength;

                            fs.Seek((long) chunk.Offset, SeekOrigin.Begin);
                            fs.Write(chunkData.Data, 0, chunkData.Data.Length);

                            sizeDownloaded += chunk.UncompressedLength;
                        }

                        fs.Dispose(); // todo; using
                        var p = CalculateProgress(sizeDownloaded, completeDownloadSize);
                        var now2 = Tools.Generic.GetCurrentUtcDateTime;
                        long? s = null;
                        if (lastBytes != 0) {
                            var timeSpan = now2 - lastTime;
                            var bytesChange = sizeDownloaded - lastBytes;
                            if (timeSpan.TotalMilliseconds > 0)
                                s = (long) (bytesChange/(timeSpan.TotalMilliseconds/1000.0));
                        }
                        lastBytes = sizeDownloaded;
                        lastTime = now2;
                        _config.Progress(s, p);
                        Log($"{p,6:#00.00}% {fileFinalPath}");
                    });

                ConfigStore.TheConfig.LastManifests[depot.ID] = depot.ManifestId;
                ConfigStore.Save();
                stagingDir.DirectoryInfo.Delete(true);

                Log(
                    $"Depot {depot.ID} - Downloaded {depotBytesCompressed} bytes ({depotBytesUncompressed} bytes uncompressed)");
            }
            Log(
                $"Total downloaded: {totalBytesCompressed} bytes ({totalBytesUncompressed} bytes uncompressed) from {depots.Count} depots");
        }

        private static float CalculateProgress(ulong sizeDownloaded, ulong completeDownloadSize)
            => sizeDownloaded/(float) completeDownloadSize*100.0f;

        private sealed class DepotDownloadInfo
        {
            public DepotDownloadInfo(uint depotid, ulong manifestId, IAbsoluteDirectoryPath installDir,
                string contentName) {
                ID = depotid;
                ManifestId = manifestId;
                InstallDir = installDir;
                ContentName = contentName;
            }

            public byte[] DepotKey { get; set; }

            public uint ID { get; }
            public IAbsoluteDirectoryPath InstallDir { get; }
            public string ContentName { get; }

            public ulong ManifestId { get; }
        }

        private class ChunkMatch
        {
            public ChunkMatch(ProtoManifest.ChunkData oldChunk, ProtoManifest.ChunkData newChunk) {
                OldChunk = oldChunk;
                NewChunk = newChunk;
            }

            public ProtoManifest.ChunkData OldChunk { get; }
            public ProtoManifest.ChunkData NewChunk { get; }
        }
    }
}