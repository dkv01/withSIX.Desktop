// <copyright company="SIX Networks GmbH" file="BuildFlags.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Core
{
    public static class BuildFlags
    {
        static BuildFlags() {
#if MAIN_RELEASE
#elif BETA_RELEASE
#elif NIGHTLY_RELEASE
    Staging = true;
#elif DEBUG
            Staging = true;
            DevBuild = true;
#else
            Staging = false;
            DevBuild = false;
#endif

#if MAIN_RELEASE
            Type = ReleaseType.Stable;
#elif BETA_RELEASE
            Type = ReleaseType.Beta;
#elif NIGHTLY_RELEASE
            Type = ReleaseType.Alpha;
#elif DEBUG
            Type = ReleaseType.Dev;
#else
            // TODO: Handle the Type on the Entrypoint, so that we can set it from Electron instead!

            Type = ReleaseType.Stable; // Modify as see fit
#endif
        }

        public static bool Staging { get; }
        public static bool DevBuild { get; set; }
        public static ReleaseType Type { get; set; }
        // Don't use .0, or it will make the wrong nupkg
        public const string Version = "1.9.0.1";
        public const string ProductVersion = "1.9.0";
    }

    public enum ReleaseType
    {
        Dev,
        Alpha,
        Beta,
        Stable
    }
}