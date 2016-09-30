// <copyright company="SIX Networks GmbH" file="PathValidator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.IO;
using System.Linq;
using NDepend.Path;

namespace withSIX.Play.Core.Games.Legacy
{
    public interface IModPathValidator
    {
        bool Validate(string path);
    }

    public abstract class PathValidator
    {
        static bool IsValidPath(string path) => Tools.FileUtil.IsValidRootedPath(path);

        protected static bool HasSubFolder(string path, string subFolder) => Directory.Exists(Path.Combine(path, subFolder));

        protected static bool ValidateBasics(string path) => IsValidPath(path) && Directory.Exists(path);
    }

    public class ArmaModPathValidator : PathValidator, IModPathValidator
    {
        static readonly string[] gameDataDirectories = {"addons", "dta", "common", "dll"};

        public bool Validate(string path) => ValidateBasics(path) && ValidateSpecials(path);

        public bool Validate(IAbsoluteDirectoryPath path) => Validate(path.ToString());

        static bool ValidateSpecials(string path) => gameDataDirectories.Any(dir => HasSubFolder(path, dir));
    }

    public class Homeworld2ModFileValidator : IModPathValidator
    {
        public bool Validate(string path) => path.EndsWith(".big");
    }
}