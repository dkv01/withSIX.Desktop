// <copyright company="SIX Networks GmbH" file="IStatus.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Sync.Core.Legacy.Status;

namespace withSIX.Sync.Core.Transfer
{
    public interface IStatus : ITransferStatus, IComparable<IStatus>
    {
        StatusRepo Repo { get; }
        string RealObject { get; set; }
    }
}