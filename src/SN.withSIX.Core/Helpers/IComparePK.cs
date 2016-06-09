// <copyright company="SIX Networks GmbH" file="IComparePK.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace SN.withSIX.Core.Helpers
{
    public interface IComparePK<in T>
    {
        [Obsolete("Use IEquatable?")]
        bool ComparePK(object other);

        [Obsolete("Use IEquatable?")]
        bool ComparePK(T other);
    }
}