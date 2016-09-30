// <copyright company="SIX Networks GmbH" file="INoteStorage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy.Missions;
using withSIX.Play.Core.Games.Legacy.Mods;

namespace withSIX.Play.Core.Infrastructure
{
    public interface INoteStorage
    {
        string GetNotes(Server server);
        string GetNotes(MissionBase mission);
        string GetNotes(Collection collection);
        void SetNotes(Server server, string text);
        void SetNotes(MissionBase mission, string text);
        void SetNotes(Collection collection, string text);
        bool HasNotes(Server server);
        bool HasNotes(MissionBase mission);
        bool HasNotes(Collection collection);
    }
}