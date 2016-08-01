using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ProtoBuf;

namespace SN.withSIX.Steam.Core.SteamKit.DepotDownloader
{
    [ProtoContract]
    public class ConfigStore
    {
        public static ConfigStore TheConfig;

        string FileName;

        ConfigStore() {
            LastManifests = new Dictionary<uint, ulong>();
            SentryData = new Dictionary<string, byte[]>();
            ContentServerPenalty = new ConcurrentDictionary<string, int>();
        }

        [ProtoMember(1)]
        public Dictionary<uint, ulong> LastManifests { get; private set; }

        [ProtoMember(3, IsRequired = false)]
        public Dictionary<string, byte[]> SentryData { get; private set; }

        [ProtoMember(4, IsRequired = false)]
        public ConcurrentDictionary<string, int> ContentServerPenalty { get; private set; }

        static bool Loaded
        {
            get { return TheConfig != null; }
        }

        public static void LoadFromFile(string filename) {
            if (Loaded)
                throw new Exception("Config already loaded");

            if (File.Exists(filename)) {
                using (var fs = File.Open(filename, FileMode.Open))
                using (var ds = new DeflateStream(fs, CompressionMode.Decompress))
                    TheConfig = Serializer.Deserialize<ConfigStore>(ds);
            } else
                TheConfig = new ConfigStore();

            TheConfig.FileName = filename;
        }

        public static void Save() {
            if (!Loaded)
                throw new Exception("Saved config before loading");

            using (var fs = File.Open(TheConfig.FileName, FileMode.Create))
            using (var ds = new DeflateStream(fs, CompressionMode.Compress))
                Serializer.Serialize(ds, TheConfig);
        }
    }
}