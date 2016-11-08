// <copyright company="SIX Networks GmbH" file="SourceQueryParser.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using withSIX.Api.Models.Extensions;

namespace GameServerQuery.Parsers
{
    public class SourceQueryParser : IServerQueryParser
    {
        public ServerQueryResult ParsePackets(IResult r) {
            var s = ParseSettings(r.ReceivedPackets["settings"]);
            s.Address = r.Endpoint; // todo
            if (r.ReceivedPackets.ContainsKey("rules"))
                s.Rules = r.ReceivedPackets["rules"];

            return new SourceServerQueryResult(r.Endpoint, s) {
                Players =
                    r.ReceivedPackets.ContainsKey("players")
                        ? ParsePlayers(r.ReceivedPackets["players"]).ToList()
                        : new List<Player>(),
                Ping = r.Ping
            };
        }

        static SourceParseResult ParseSettings(byte[] info) {
            var r = new Reader(info);
            r.Skip(5);
            var settings = new SourceParseResult();
            settings.Protocol = r.ReadAsInt();
            settings.Name = r.ReadStringUntil();
            settings.Map = r.ReadStringUntil();
            settings.Folder = r.ReadStringUntil();
            settings.Game = r.ReadStringUntil();
            settings.AppId = r.ReadShort();
            settings.CurrentPlayers = r.ReadAsInt();
            settings.MaxPlayers = r.ReadAsInt();
            settings.BotCount = r.ReadAsInt();
            settings.ServerType = r.ReadAsInt();
            settings.Environment = r.ReadAsInt();
            settings.Visibility = r.ReadAsInt();
            settings.Vac = r.ReadAsInt();
            //if The Ship, additional fields:
            //_results.mode = reply[pos++];
            //_results.witnesses = reply[pos++];
            //_results.duration = reply[pos++];
            settings.Version = r.ReadStringUntil();
            var edf = r.ReadByte();
            //settings.edf = edf.ToString(CultureInfo.InvariantCulture);
            if ((edf & 0x80) != 0)
                settings.Port = r.ReadShort();
            if ((edf & 0x10) != 0)
                settings.SteamId = r.ReadLong();
            if ((edf & 0x40) != 0) {
                settings.TvPort = r.ReadShort();
                settings.TvName = r.ReadStringUntil();
            }
            if ((edf & 0x20) != 0) {
                settings.Keywords = r.ReadStringUntil();
            }
            //gameID = full appID if was truncated in previous field (not always the case)
            if ((edf & 0x01) != 0)
                settings.AppId = r.ReadLong();

            return settings;
        }

        public static Dictionary<string, string> ParseRules(byte[] rules) {
            var r = new Reader(rules);
            r.Skip(5);
            var ruleCount = r.ReadShort();
            var dict = new Dictionary<string, string>();
            r.Skip(2);
            r.WhileNotOutOfBounds(ruleCount, () => {
                var rule = r.ReadStringUntil();
                var value = r.ReadStringUntil();
                dict[rule] = value;
            });

            return dict;
        }

        static Regex GetRx(string keyWord) {
            Regex rx;
            if (rxCache.TryGetValue(keyWord, out rx))
                return rx;
            return
                rxCache[keyWord] =
                    new Regex(@"^" + keyWord + @":([0-9]+)\-([0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        static readonly ConcurrentDictionary<string, Regex> rxCache = new ConcurrentDictionary<string, Regex>();

        public static IEnumerable<string> GetList(IEnumerable<KeyValuePair<string, string>> dict, string keyWord) {
            var rx = GetRx(keyWord);
            return string.Join("", (from kvp in dict.Where(x => x.Key.StartsWith(keyWord))
                                    let w = rx.Match(kvp.Key)
                                    where w.Success
                                    select new { Index = w.Groups[1].Value.TryInt(), Total = w.Groups[2].Value.TryInt(), kvp.Value })
                    .OrderBy(x => x.Index).SelectMany(x => x.Value))
                .Split(';')
                .Where(x => !string.IsNullOrWhiteSpace(x));
        }

        static Player[] ParsePlayers(byte[] players) {
            //TODO: Player doesn't have same info for different game/server types
            var pos = 5; //skip header
            var cnt = players[pos++];
            var playerAr = new Player[cnt];
            for (var i = 0; i < cnt; i++) {
                if (pos >= players.Length)
                    break;
                // ReSharper disable once UnusedVariable
                var pnum = players[pos++];
                //TODO: index is always zero, why? Defined as "Index of player chunk starting from 0." should be usable below
                var start = pos;
                while (players[pos++] != 0x00) {}
                var name = Encoding.UTF8.GetString(players, start, pos - start - 1);
                var score = BitConverter.ToInt32(players, pos);
                pos += 4;
                var duration = (int) BitConverter.ToSingle(players, pos); //in seconds, dropped float precision
                pos += 4;
                playerAr[i] = new SourcePlayer(name, score, TimeSpan.FromSeconds(duration));
                //use i instead of pnum
            }

            return playerAr;
        }

        class Reader : ByteArrayReader
        {
            public Reader(byte[] b) : base(b) {}
        }
    }

    public class ParseResult
    {
        public IPEndPoint Address { get; set; }
    }

    public class SourceParseResult : ParseResult
    {
        public int Protocol { get; set; }
        public string Name { get; set; }
        public string Map { get; set; }
        public string Folder { get; set; }
        public string Game { get; set; }
        public long AppId { get; set; }
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public int BotCount { get; set; }
        public int ServerType { get; set; }
        public int Environment { get; set; }
        public int Visibility { get; set; }
        public int Vac { get; set; }
        public string Version { get; set; }
        public int Port { get; set; }
        public long SteamId { get; set; }
        public int TvPort { get; set; }
        public string TvName { get; set; }
        public string Keywords { get; set; }
        public byte[] Rules { get; set; }
    }

    public abstract class Player
    {
        protected Player(string name) {
            Name = name;
        }

        public string Name { get; }

        public virtual bool ComparePK(object obj) {
            var emp = obj as Player;
            if (emp != null)
                return ComparePK(emp);
            return false;
        }

        public bool ComparePK(Player other) {
            if (other == null)
                return false;
            if (ReferenceEquals(other, this))
                return true;

            // Otherwise we break syncing based on gamespy vs source
            if (other.GetType() != GetType())
                return false;

            return (other.Name != null) && other.Name.Equals(Name);
        }
    }


    public class SourcePlayer : Player
    {
        public SourcePlayer(string name, int score, TimeSpan duration)
            : base(name) {
            Duration = duration;
            Score = score;
        }

        public int Score { get; private set; }
        public TimeSpan Duration { get; private set; }
    }
}