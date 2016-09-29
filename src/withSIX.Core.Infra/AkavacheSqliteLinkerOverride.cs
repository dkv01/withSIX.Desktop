// <copyright company="SIX Networks GmbH" file="AkavacheSqliteLinkerOverride.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using Akavache.Sqlite3;

// Note: This class file is *required* for iOS to work correctly, and is 
// also a good idea for Android if you enable "Link All Assemblies".

namespace withSIX.Core.Infra
{
    [Preserve]
    public static class LinkerPreserve
    {
        static LinkerPreserve() {
            throw new Exception(typeof(SQLitePersistentBlobCache).FullName);
        }
    }


    public class PreserveAttribute : Attribute {}
}