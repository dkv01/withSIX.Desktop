﻿// <copyright company="SIX Networks GmbH" file="TransferProtocol.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Sync.Core.Transfer.Specs;

namespace SN.withSIX.Sync.Core.Transfer.Protocols
{
    public abstract class TransferProtocol
    {
        public abstract IEnumerable<string> Schemes { get; }

        protected void ConfirmSchemeSupported(string scheme) {
            if (!Schemes.Contains(scheme))
                throw new ProtocolMismatch();
        }

        protected static string CreateTransferExceptionMessage(TransferSpec spec) =>
            $"After {spec.Progress.Progress}% ({spec.Progress.FileSizeTransfered}B) for {spec.Uri.AuthlessUri()}";
    }
}