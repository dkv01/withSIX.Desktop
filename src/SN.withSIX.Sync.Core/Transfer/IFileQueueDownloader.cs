// <copyright company="SIX Networks GmbH" file="IFileQueueDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Sync.Core.Legacy;

namespace SN.withSIX.Sync.Core.Transfer
{
    public interface IFileQueueDownloader
    {
        Task DownloadAsync(FileQueueSpec spec);
        Task DownloadAsync(FileQueueSpec spec, CancellationToken token);
    }

    public class FileQueueSpec
    {
        public IDictionary<FileFetchInfo, ITransferStatus> Files { get; }
        public IAbsoluteDirectoryPath Location { get; }

        public FileQueueSpec(IDictionary<FileFetchInfo, ITransferStatus> files,
            IAbsoluteDirectoryPath location) {
            Files = files;
            Location = location;
        }

        public FileQueueSpec(IDictionary<FileFetchInfo, ITransferStatus> files,
            string location) : this(files, location.ToAbsoluteDirectoryPath()) {}
    }
}