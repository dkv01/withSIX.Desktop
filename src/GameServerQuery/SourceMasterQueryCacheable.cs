// <copyright company="SIX Networks GmbH" file="SourceMasterQueryCacheable.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GameServerQuery.Extensions;
using NDepend.Path;

namespace GameServerQuery
{
    public class SourceMasterQueryCacheable : SourceMasterQuery, IMasterServerQuery
    {
        readonly IAbsoluteDirectoryPath _cachePath;

        public SourceMasterQueryCacheable(string serverBrowserTag, IAbsoluteDirectoryPath cachePath,
            Region region = Region.All)
            : base(serverBrowserTag, region) {
            _cachePath = cachePath;
        }

        public override async Task<IEnumerable<ServerQueryResult>> GetParsedServers(bool forceLocal = false,
            int limit = 0) {
            var serversResult = forceLocal ? null : await RetrieveAsync(0).ConfigureAwait(false);
            await Task.Run(() => {
                var cache = GetCacheFilePath();
                if (serversResult == null || serversResult.Count == 0) {
                    try {
                        var en = File.ReadLines(cache.ToString(), Encoding.UTF8);
                        //new List<string>(File.ReadLines(cache, Encoding.UTF8));
                        if (limit > 0)
                            en = en.Take(limit);
                        serversResult = en.Select(x => x.ToIPEndPoint()).ToList();
                    } catch (Exception e) {
                        serversResult = new List<IPEndPoint>();
                    }
                } else {
                    try {
                        File.WriteAllLines(cache.ToString(), serversResult.Select(x => x.ToString()), Encoding.UTF8);
                    } catch (Exception e) {}
                    if (limit > 0)
                        serversResult = serversResult.Take(limit).ToList();
                }
            }).ConfigureAwait(false);
            return serversResult.Select(CreateServerDictionary);
        }

        IAbsoluteFilePath GetCacheFilePath() {
            var cachePath = _cachePath.GetChildDirectoryWithName("Serverlists");
            cachePath.DirectoryInfo.Create();
            return cachePath.GetChildFileWithName(String.Format("source_{0}.txt", ServerBrowserTag));
        }
    }
}