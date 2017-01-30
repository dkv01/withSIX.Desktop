// <copyright company="SIX Networks GmbH" file="TransferSpec.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using NDepend.Path;

namespace withSIX.Sync.Core.Transfer.Specs
{
    public abstract class TransferSpec
    {
        protected TransferSpec(Uri uri, IAbsoluteFilePath localFile, ITransferProgress progress) {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (localFile == null) throw new ArgumentNullException(nameof(localFile));

            Uri = uri;
            LocalFile = localFile;
            Progress = progress ?? new TransferProgress();
        }

        public CancellationToken CancellationToken { get; set; }
        public IAbsoluteFilePath LocalFile { get; }
        public ITransferProgress Progress { get; }
        public Uri Uri { get; }
        public Func<IAbsoluteFilePath, bool> Verification { get; set; }
        public IAbsoluteFilePath ExistingFile { get; set; }
    }
}