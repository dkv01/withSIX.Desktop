// <copyright company="SIX Networks GmbH" file="MultiMirrorFileDownloadSpec.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using NDepend.Path;
using SN.withSIX.Core;

namespace SN.withSIX.Sync.Core.Transfer.Specs
{
    public class MultiMirrorFileDownloadSpec
    {
        public MultiMirrorFileDownloadSpec(string remoteFile, IAbsoluteFilePath localFile) {
            Contract.Requires<ArgumentNullException>(remoteFile != null);
            Contract.Requires<ArgumentNullException>(localFile != null);
            Contract.Requires<ArgumentException>(!remoteFile.Contains(@"\"));
            RemoteFile = remoteFile;
            LocalFile = localFile;
        }

        public MultiMirrorFileDownloadSpec(string remoteFile, IAbsoluteFilePath localFile,
            Func<IAbsoluteFilePath, bool> confirmValidity)
            : this(remoteFile, localFile) {
            Contract.Requires<ArgumentNullException>(confirmValidity != null);
            Verification = confirmValidity;
        }

        public IAbsoluteFilePath LocalFile { get; }
        public string RemoteFile { get; }

        public ITransferStatus Progress { get; set; }
        public Func<IAbsoluteFilePath, bool> Verification { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public IAbsoluteFilePath ExistingFile { get; set; }

        public Uri GetUri(Uri host)
            => Tools.Transfer.JoinUri(host, Tools.Transfer.EncodePathIfRequired(host, RemoteFile));

        public void Start() {
            if (Progress != null) {
                Progress.StartOutput(LocalFile.ToString());
            }
        }

        public void End() => Progress?.EndOutput(LocalFile.ToString());

        public void Fail() => Progress?.FailOutput(LocalFile.ToString());

        public void UpdateHost(Uri host) {
            if (Progress != null)
                Progress.Info = host.ToString();
        }
    }
}