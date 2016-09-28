// <copyright company="SIX Networks GmbH" file="ServerInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SN.withSIX.Mini.Core.Games
{
    // DTO-like, shared with API
    public class ServerInfo
    {
        public IPEndPoint Address { get; set; }
        public string Name { get; set; }
        public long Ping { get; set; }
        public int NumPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string MissionName { get; set; }
        public string MapName { get; set; }
        public IPEndPoint ServerAddress { get; set; }
        public List<string> Mods { get; set; }
        public bool PasswordRequired { get; set; }
        public Version GameVersion { get; set; }
        public int VerifySignatures { get; set; }
        public int SvBattleye { get; set; }
        public int? ReqBuild { get; set; }
        public int Difficulty { get; set; }
        public bool IsDedicated { get; set; }
        public int GameState { get; set; }
        public string GameType { get; set; }
        public ServerPlatform ServerPlatform { get; set; }
        public int? RequiredVersion { get; set; }
        public int? Language { get; set; }
        public Coordinates Coordinates { get; set; }
        public int Status { get; set; }

        public List<Player> Players { get; set; }
    }

    public class ServerInfo<T> : ServerInfo
    {
        public T Details { get; set; }
    }

    public class Player
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public TimeSpan Duration { get; set; }
    }


    public enum ServerPlatform
    {
        Windows,
        Linux
    }

    public class Coordinates
    {
        public Coordinates(double longitude, double latitude) {
            Longitude = longitude;
            Latitude = latitude;
        }

        public double Longitude { get; }
        public double Latitude { get; }
    }

    public interface IQueryServers
    {
        Task<List<IPEndPoint>> GetServers(CancellationToken cancelToken);

        Task<List<ServerInfo>> GetServerInfos(IReadOnlyCollection<IPEndPoint> addresses,
            bool inclExtendedDetails = false);
    }
}