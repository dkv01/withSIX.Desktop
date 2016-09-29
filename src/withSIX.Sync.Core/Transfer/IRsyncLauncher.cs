// <copyright company="SIX Networks GmbH" file="IRsyncLauncher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using withSIX.Core.Services.Infrastructure;
using withSIX.Sync.Core.Transfer.Protocols.Handlers;

namespace withSIX.Sync.Core.Transfer
{
    public interface IRsyncLauncher
    {
        ProcessExitResultWithOutput Run(string source, string destination, RsyncOptions options = null);

        ProcessExitResultWithOutput RunAndProcess(ITransferProgress progress, string source, string destination,
            RsyncOptions options = null);

        Task<ProcessExitResultWithOutput> RunAndProcessAsync(ITransferProgress progress, string source,
            string destination,
            RsyncOptions options = null);

        ProcessExitResultWithOutput RunAndProcess(ITransferProgress progress, string source, string destination,
            CancellationToken token,
            RsyncOptions options = null);

        Task<ProcessExitResultWithOutput> RunAndProcessAsync(ITransferProgress progress, string source,
            string destination, CancellationToken token,
            RsyncOptions options = null);
    }
}