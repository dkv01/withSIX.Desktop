// <copyright company="SIX Networks GmbH" file="IMultiMirrorFileDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using withSIX.Sync.Core.Transfer.Specs;

namespace withSIX.Sync.Core.Transfer
{
    public interface IMultiMirrorFileDownloader
    {
        void Download(MultiMirrorFileDownloadSpec spec);
        Task DownloadAsync(MultiMirrorFileDownloadSpec spec);
        void Download(MultiMirrorFileDownloadSpec spec, CancellationToken token);
        Task DownloadAsync(MultiMirrorFileDownloadSpec spec, CancellationToken token);
    }
}