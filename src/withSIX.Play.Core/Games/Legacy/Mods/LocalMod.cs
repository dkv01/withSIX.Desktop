// <copyright company="SIX Networks GmbH" file="LocalMod.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using NDepend.Path;

using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Core.Validators;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Core.Games.Legacy.Mods
{
    
    public class LocalMod : Mod
    {
        public LocalMod(Guid id) : base(id) {
            Categories = new[] {Common.DefaultCategory};
        }

        public bool IsLocal => true;

        public override string GetSerializationString() {
            var path = CustomPath;
            return path?.GetChildDirectoryWithName(Name).ToString() ?? Name;
        }

        public static LocalMod FromStringIfValid(string modPath, ISupportModding game) {
            if (!(!string.IsNullOrWhiteSpace(modPath))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(modPath)");
            if (game == null) throw new ArgumentNullException(nameof(game));

            if (modPath.IsValidAbsoluteFilePath()) {
                var filePath = modPath.ToAbsoluteFilePath();
                return CreateLocalMod(filePath.FileName, filePath.ParentDirectoryPath, game);
            }

            if (modPath.IsValidAbsoluteDirectoryPath()) {
                var dirPath = modPath.ToAbsoluteDirectoryPath();
                return CreateLocalMod(dirPath.DirectoryName, dirPath.ParentDirectoryPath, game);
            }

            if (!FileNameValidator.IsValidName(modPath))
                return null;

            // TODO: Horrible non-determinism; is it a file, is it a folder, is it located in the game or mod dir?
            var modPaths = game.ModPaths;
            if (modPaths.IsValid) {
                try {
                    if (modPaths.Path.GetChildDirectoryWithName(modPath).Exists)
                        return CreateLocalMod(modPath, modPaths.Path, game);
                } catch (Exception) {}
                try {
                    if (modPaths.Path.GetChildFileWithName(modPath).Exists)
                        return CreateLocalMod(modPath, modPaths.Path, game);
                } catch (Exception) {}
            }

            var installedState = game.InstalledState;
            if (installedState.IsInstalled) {
                try {
                    if (installedState.Directory.GetChildDirectoryWithName(modPath).Exists)
                        return CreateLocalMod(modPath, installedState.Directory, game);
                } catch (Exception) {}
                try {
                    if (installedState.Directory.GetChildFileWithName(modPath).Exists)
                        return CreateLocalMod(modPath, installedState.Directory, game);
                } catch (Exception) {}
            }

            return CreateLocalMod(modPath, null, game);
        }

        public static LocalMod FromLocalModInfo(LocalModInfo info, ISupportModding game) {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (game == null) throw new ArgumentNullException(nameof(game));
            return CreateLocalMod(info.Name, info.Path, game);
        }

        static LocalMod CreateLocalMod(string name, IAbsoluteDirectoryPath localModPath, ISupportModding game) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (game == null) throw new ArgumentNullException(nameof(game));

            var localMod = new LocalMod(System.Guid.Empty) {
                Name = name,
                CustomPath = localModPath
            };
            localMod.Controller.UpdateState(game);
            return localMod;
        }

        public override Uri ProfileUrl() {
            throw new NotSupportedException("Local mods have no profile");
        }

        protected override string GetSlugType() => typeof(Mod).Name.ToUnderscore() + "s";
    }
}