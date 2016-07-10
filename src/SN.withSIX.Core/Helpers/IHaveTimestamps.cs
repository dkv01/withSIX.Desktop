// <copyright company="SIX Networks GmbH" file="IHaveTimestamps.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace SN.withSIX.Core.Helpers
{
    public interface IHaveTimestamps
    {
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}