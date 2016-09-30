// <copyright company="SIX Networks GmbH" file="NoteStorage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using System.Text;
using NDepend.Path;
using withSIX.Core;
using withSIX.Core.Infra.Services;
using withSIX.Play.Core;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy.Missions;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Play.Core.Infrastructure;
using withSIX.Api.Models.Extensions;

namespace withSIX.Play.Infra.Data.Services
{
    [Obsolete("Convert to Akavache")]
    public class NoteStorage : INoteStorage, IInfrastructureService
    {
        public string GetNotes(Server server) => GetNotes(server, "Server");

        public string GetNotes(MissionBase mission) => GetNotes(mission, "Mission");

        public string GetNotes(Collection collection) => GetNotes(collection, "Collection");

        public void SetNotes(Server server, string text) {
            SetNotes(server, "Server", text);
        }

        public void SetNotes(MissionBase mission, string text) {
            SetNotes(mission, "Server", text);
        }

        public void SetNotes(Collection collection, string text) {
            SetNotes(collection, "Collection", text);
        }

        public bool HasNotes(Server server) => HasNotes(server, "Server");

        public bool HasNotes(MissionBase mission) => HasNotes(mission, "Mission");

        public bool HasNotes(Collection collection) => HasNotes(collection, "Collection");

        string GetNotes(IHaveNotes note, string type) {
            var fileName = GetNoteFileName(note, type);
            return fileName.Exists ? File.ReadAllText(fileName.ToString(), Encoding.UTF8) : String.Empty;
        }

        void SetNotes(IHaveNotes note, string type, string text) {
            var fileName = GetNoteFileName(note, type);
            if (string.IsNullOrWhiteSpace(text)) {
                if (fileName.Exists)
                    Tools.FileUtil.Ops.DeleteWithRetry(fileName.ToString());
            } else {
                var noteDir = GetNoteDirectory(type);
                noteDir.MakeSurePathExists();
                File.WriteAllText(fileName.ToString(), text, Encoding.UTF8);
            }
        }

        bool HasNotes(IHaveNotes note, string type) => GetNoteFileName(note, type).Exists;

        IAbsoluteDirectoryPath GetNoteDirectory(string type) => Common.Paths.NotePath.GetChildDirectoryWithName(type + "s");

        IAbsoluteFilePath GetNoteFileName(IHaveNotes noter, string type) => GetNoteDirectory(type)
        .GetChildFileWithName(string.Format(type + "_{0}.txt",
            noter.NoteName.Replace(".", "_").Replace(":", "_")));
    }
}