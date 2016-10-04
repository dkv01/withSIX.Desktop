// <copyright company="SIX Networks GmbH" file="SourceQueryParser.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;

namespace GameServerQuery.Parsers
{
    public class SourceQueryParser : IServerQueryParser
    {
        public ServerQueryResult ParsePackets(ServerQueryState state) {
            var receivedPackets = state.ReceivedPackets;
            if (receivedPackets.Count != state.MaxPackets)
                throw new Exception("Wrong number of packets");
            return ParsePackets(state.Server.Address, state.ReceivedPackets, state.Pings);
        }

        public ServerQueryResult ParsePackets(IPEndPoint address, IReadOnlyList<byte[]> receivedPackets, List<int> pings) {
            var settings = new Dictionary<string, object>();
            ParseSettings(settings, receivedPackets[0]);
            ParseRules(settings, receivedPackets[1]);

            return new SourceServerQueryResult(address, settings) {
                Players =
                    receivedPackets.Count == 3
                        ? ParsePlayers(receivedPackets[2]).ToList()
                        : new List<Player>(),
                Ping = pings.Any() ? Convert.ToInt32(pings.Average()) : ServerQueryState.MagicPingValue
            };
        }

        class Reader : ByteArrayReader
        {
            public Reader(byte[] b) : base(b) {}

            //public string ReadByteString() => ReadByte().ToString(CultureInfo.InvariantCulture);
        }

        static void ParseSettings(IDictionary<string, object> settings, byte[] info) {
            var r = new Reader(info);
            r.Skip(5);
            settings["protocol"] = r.ReadAsInt();
            settings["name"] = r.ReadStringUntil();
            settings["map"] = r.ReadStringUntil();
            settings["folder"] = r.ReadStringUntil();
            settings["game"] = r.ReadStringUntil();
            settings["appId"] = r.ReadShort().ToString(CultureInfo.InvariantCulture);
            settings["playerCount"] = r.ReadAsInt();
            settings["playerMax"] = r.ReadAsInt();
            settings["botCount"] = r.ReadAsInt();
            settings["serverType"] = r.ReadAsInt();
            settings["environment"] = r.ReadAsInt();
            settings["visibility"] = r.ReadAsInt();
            settings["vac"] = r.ReadAsInt();
            //if The Ship, additional fields:
            //_results.mode = reply[pos++];
            //_results.witnesses = reply[pos++];
            //_results.duration = reply[pos++];
            settings["version"] = r.ReadStringUntil();
            var edf = r.ReadByte();
            settings["edf"] = edf.ToString(CultureInfo.InvariantCulture);
            if ((edf & 0x80) != 0)
                settings["port"] = r.ReadShort();
            if ((edf & 0x10) != 0)
                settings["steamId"] = r.ReadLong();
            if ((edf & 0x40) != 0) {
                settings["tvPort"] = r.ReadShort();
                settings["tvName"] = r.ReadStringUntil();
            }
            if ((edf & 0x20) != 0) {
                settings["keywords"] = r.ReadStringUntil();
            }
            //gameID = full appID if was truncated in previous field (not always the case)
            if ((edf & 0x01) != 0)
                settings["gameId"] = r.ReadLong().ToString(CultureInfo.InvariantCulture);
            //TODO: find out what the extra data in this packet is from DayZ source
        }

        static void ParseRules(Dictionary<string, object> settings, byte[] rules) {
            var sb = new StringBuilder();
            var pos = 5; //skip header
            var ruleCount = BitConverter.ToInt16(rules, pos);
            pos += 2;
            for (var i = 0; i < ruleCount; i++) {
                if (pos >= rules.Length)
                    break;
                var start = pos;
                while (rules[pos++] != 0x00) {}
                var rule = Encoding.UTF8.GetString(rules, start, pos - start - 1);
                sb.Append(rule + ",");
                start = pos;
                while (rules[pos++] != 0x00) {}
                var value = Encoding.UTF8.GetString(rules, start, pos - start - 1);
                settings[rule] = value;
            }
            settings["ruleNames"] = sb.Length > 0 ? sb.ToString(0, sb.Length - 1) : string.Empty;
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