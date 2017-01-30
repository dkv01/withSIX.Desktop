// <copyright company="SIX Networks GmbH" file="CopyPropertiesExtension.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using AutoMapper;
using withSIX.Core.Helpers;

namespace withSIX.Core.Extensions
{
    public static class CopyPropertiesExtension
    {
        static Mapper Mapper = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMissingTypeMaps = true; }));

        public static void CopyProperties(this object src, object dest, string[] extraExclusions = null) {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (dest == null) throw new ArgumentNullException(nameof(dest));

            if (ReferenceEquals(src, dest))
                return;
            if (src is ICopyProperties)
                throw new NotSupportedException("Dont support this interface right now");
            Mapper.Map(src, dest);
        }

        public static void CopyFromProperties(this object dest, object src, string[] extraExclusions = null) {
            src.CopyProperties(dest, extraExclusions);
        }
    }
}