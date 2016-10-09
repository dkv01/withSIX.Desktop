// <copyright company="SIX Networks GmbH" file="RV.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using NDepend.Helpers;

namespace GameServerQuery.Games.RV
{
    public enum GameState
    {
        Unknown,
        Waiting,
        Unknown2,
        Creating,
        Unknown3,
        Loading,
        Briefing,
        Playing,
        Unknown4,
        Debriefing
    }

    public enum AiLevel
    {
        Novice,
        Normal,
        Expert,
        Custom
    }

    public enum Difficulty
    {
        Recruit,
        Regular,
        Veteran,
        Custom
    }

    public enum SessionState
    {
        None,
        SelectingMission,
        EditingMission,
        AssigningRoles,
        SendingMission,
        LoadingGame,
        Briefing,
        Playing,
        Debriefing,
        MissionAborted
    }

    [Flags]
    public enum Dlcs
    {
        Apex = 0x10,
        Helicopters = 4,
        Karts = 2,
        Marksmen = 8,
        None = 0,
        Tanoa = 0x20,
        Zeus = 1
    }

    public enum HelicopterFlightModel
    {
        Basic,
        Advanced
    }

    public enum BattlEye
    {
        Off,
        On
    }

    public class Coordinates
    {
        public Coordinates(double longitude, double latitude) {
            Longitude = longitude;
            Latitude = latitude;
        }

        public double Longitude { get; protected set; }
        public double Latitude { get; protected set; }
    }

    public class GameTags
    {
        public int? AllowedFilePatching { get; set; }

        public bool? BattlEye { get; set; }

        public int? Build { get; set; }

        public string Country { get; set; }

        public bool? Dedicated { get; set; }

        public int? Difficulty { get; set; }

        public bool? EqualModRequired { get; set; }

        public string GameType { get; set; }

        public int? GlobalHash { get; set; }

        public int? Language { get; set; }

        public bool? Lock { get; set; }

        public float? Param1 { get; set; }

        public float? Param2 { get; set; }

        public char? Platform { get; set; }

        public int? ServerState { get; set; }

        public int? TimeRemaining { get; set; }

        public bool? VerifySignatures { get; set; }

        public int? Version { get; set; }

        public static GameTags Parse(string value) {
            var tags = new GameTags();
            foreach (var tuple in (from part in value.Split(',')
                where part.Length > 0
                select Tuple.Create(part[0], part.Substring(1))).ToHashSet()) {
                switch (tuple.Item1) {
                case 'b':
                    tags.BattlEye = ReadBool(tuple.Item2);
                    break;

                case 'd':
                    tags.Dedicated = ReadBool(tuple.Item2);
                    break;

                case 'e':
                    tags.TimeRemaining = ReadInt(tuple.Item2);
                    break;

                case 'f':
                    tags.AllowedFilePatching = ReadInt(tuple.Item2);
                    break;

                case 'g':
                    tags.Language = ReadInt(tuple.Item2);
                    break;

                case 'h':
                    tags.GlobalHash = ReadInt(tuple.Item2);
                    break;

                case 'l':
                    tags.Lock = ReadBool(tuple.Item2);
                    break;

                case 'm':
                    tags.EqualModRequired = ReadBool(tuple.Item2);
                    break;

                case 'n':
                    tags.Build = ReadInt(tuple.Item2);
                    break;

                case 'o':
                    tags.Country = tuple.Item2;
                    break;

                case 'p':
                    tags.Platform = ReadChar(tuple.Item2);
                    break;

                case 'r':
                    tags.Version = ReadInt(tuple.Item2);
                    break;

                case 's':
                    tags.ServerState = ReadInt(tuple.Item2);
                    break;

                case 't':
                    tags.GameType = tuple.Item2;
                    break;

                case 'v':
                    tags.VerifySignatures = ReadBool(tuple.Item2);
                    break;
                }
            }
            return tags;
        }

        private static bool? ReadBool(string value) {
            if (value == "t") {
                return true;
            }
            if (value == "f") {
                return false;
            }
            return null;
        }

        private static char? ReadChar(string value) {
            if (!string.IsNullOrWhiteSpace(value)) {
                return value[0];
            }
            return null;
        }

        private static int? ReadInt(string value) {
            int num;
            if (!int.TryParse(value, out num)) {
                return null;
            }
            return num;
        }
    }

    public enum ServerPlatform
    {
        Windows,
        Linux
    }

    public class ServerModInfo
    {
        public int Hash { get; set; }
        public bool IsOptionalOnServer { get; set; }
        public bool IsRequiredByMission { get; set; }
        public string Name { get; set; }
        public ulong PublishedId { get; set; }
    }
}