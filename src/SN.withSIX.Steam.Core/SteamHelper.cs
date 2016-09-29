// <copyright company="SIX Networks GmbH" file="SteamHelper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;
using withSIX.Steam.Core.Extensions;

namespace withSIX.Steam.Core
{
    public class SteamStuff
    {
        private readonly IAbsoluteDirectoryPath _steamPath;

        public SteamStuff(IAbsoluteDirectoryPath steamPath) {
            _steamPath = steamPath;
        }

        public KeyValue TryReadSteamConfig() {
            try {
                return ReadSteamConfig();
            } catch (ParseException ex) {
                //this.Logger().FormattedFormattedWarnException(ex, "during steam config parsing");
                return null;
            }
        }

        KeyValue ReadSteamConfig() {
            if ((_steamPath == null) || !_steamPath.Exists)
                return null;

            var steamConfigPath = _steamPath.GetChildDirectoryWithName("config").GetChildFileWithName("config.vdf");
            return steamConfigPath.Exists
                ? KeyValueHelper.LoadFromFile(steamConfigPath)
                : null;
        }
    }

    public class KeyValueHelper
    {
        public static void SaveToFile(KeyValue kv, IAbsoluteFilePath fp, bool asBinary = false) {
            MainLog.Logger.Debug($"Saving KV to file {fp}, {asBinary}");
            kv.SaveToFile(kv.ToString(), asBinary);
            MainLog.Logger.Debug($"Saved KV to file {fp}, {asBinary}");
        }

        public static async Task<KeyValue> LoadFromFileAsync(IAbsoluteFilePath fp, CancellationToken cancelToken) {
            MainLog.Logger.Debug($"Loading KV from file {fp}");
            var input = await fp.ReadTextAsync(cancelToken).ConfigureAwait(false);
            MainLog.Logger.Debug($"Loaded KV from file {fp}");
            return ParseKV(input);
        }

        public static KeyValue LoadFromFile(IAbsoluteFilePath fp) {
            MainLog.Logger.Debug($"Loading KV from file {fp}");
            var input = fp.ReadAllText();
            MainLog.Logger.Debug($"Loaded KV from file {fp}");
            return ParseKV(input);
        }

        private static KeyValue ParseKV(string input) {
            MainLog.Logger.Debug($"Parsing KV");
            var v = KeyValue.LoadFromString(input);
            MainLog.Logger.Debug($"Parsed KV");
            return v;
        }
    }

    public class SteamHelper : ISteamHelper
    {
        readonly Dictionary<uint, SteamApp> _appCache;
        readonly IEnumerable<IAbsoluteDirectoryPath> _baseInstallPaths;
        readonly bool _cache;

        public SteamHelper(KeyValue steamConfig, IAbsoluteDirectoryPath steamPath, bool cache = true) {
            _cache = cache;
            _appCache = new Dictionary<uint, SteamApp>();
            Config = steamConfig;
            SteamPath = steamPath;
            SteamFound = (SteamPath != null) && SteamPath.Exists;
            _baseInstallPaths = GetBaseInstallFolderPaths();
        }

        public KeyValue Config { get; }


        public IAbsoluteDirectoryPath SteamPath { get; }
        public bool SteamFound { get; }

        public ISteamApp TryGetSteamAppById(uint appId, bool noCache = false) {
            if (!SteamFound)
                throw new InvalidOperationException("Unable to get Steam App, Steam was not found.");
            try {
                return GetSteamAppById(appId, noCache);
            } catch (Exception e) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.FormattedWarnException(e, "Unknown Exception Attempting to get Steam App");
                return SteamApp.Default;
            }
        }

        public static SteamHelper Create()
            => new SteamHelper(new SteamStuff(SteamPathHelper.SteamPath).TryReadSteamConfig(),
                SteamPathHelper.SteamPath);

        KeyValue TryGetConfigByAppId(uint appId) {
            KeyValue apps = null;
            try {
                apps = Config.GetKeyValue("Software", "Valve", "Steam", "apps");
            } catch (KeyNotFoundException ex) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.FormattedWarnException(ex, "Config Store Invalid");
                return null;
            }
            try {
                return apps.GetKeyValue(appId.ToString());
            } catch (Exception) {
                return null;
            }
        }

        SteamApp GetSteamAppById(uint appId, bool noCache) {
            if (!noCache && _appCache.ContainsKey(appId))
                return _appCache[appId];
            var app = new SteamApp(appId, GetAppManifestLocation(appId), TryGetConfigByAppId(appId));
            if (app.InstallBase != null)
                _appCache[appId] = app;
            return app;
        }

        IAbsoluteDirectoryPath GetAppManifestLocation(uint appId)
            => _baseInstallPaths.FirstOrDefault(installPath => CheckForAppManifest(appId, installPath));

        bool CheckForAppManifest(uint appId, IAbsoluteDirectoryPath installPath)
            => installPath.GetChildDirectoryWithName("SteamApps")
                .GetChildFileWithName("appmanifest_" + appId + ".acf")
                .Exists;

        IReadOnlyList<IAbsoluteDirectoryPath> GetBaseInstallFolderPaths() {
            var list = new List<IAbsoluteDirectoryPath> {SteamPath};
            if (Config == null)
                return list.AsReadOnly();
            try {
                var kv = Config.GetKeyValue("Software", "Valve", "Steam");
                var iFolder = 1;
                string key;
                while (kv.ContainsKey(key = BuildKeyName(iFolder++)))
                    list.Add(kv[key].AsString().ToAbsoluteDirectoryPath());
            } catch (KeyNotFoundException ex) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.FormattedWarnException(ex, "Config Store Invalid");
            }
            return list.AsReadOnly();
        }

        private static string BuildKeyName(int iFolder) => "BaseInstallFolder_" + iFolder;
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class SteamApp : ISteamApp
    {
        protected SteamApp() {}

        public SteamApp(uint appId, IAbsoluteDirectoryPath installBase, KeyValue appConfig) {
            AppId = appId;
            InstallBase = installBase;
            AppConfig = appConfig;
            if (installBase != null)
                LoadManifest();
            SetAppPath();
        }

        public KeyValue AppManifest { get; private set; }
        public KeyValue AppConfig { get; }
        public static SteamApp Default { get; } = new NullSteamApp();

        public uint AppId { get; }
        public IAbsoluteDirectoryPath InstallBase { get; }
        public IAbsoluteDirectoryPath AppPath { get; private set; }
        public virtual bool IsValid => true;

        public string GetInstallDir() {
            if ((AppConfig != null) && AppConfig.ContainsKey("installdir"))
                return AppConfig["installdir"].AsString();
            try {
                return AppManifest?.GetKeyValue("installdir").AsString();
            } catch (KeyNotFoundException ex) {
                if (Common.Flags.Verbose)
                    MainLog.Logger.FormattedWarnException(ex, "AppManifest Invalid ({0})".FormatWith(AppId));
            }
            return null;
        }

        void SetAppPath() {
            var installDir = GetInstallDir();
            AppPath = InstallBase.GetChildDirectoryWithName(@"SteamApps\Common\" + installDir);
        }

        void LoadManifest() {
            var configPath =
                InstallBase.GetChildDirectoryWithName("SteamApps").GetChildFileWithName("appmanifest_" + AppId + ".acf");
            AppManifest = KeyValueHelper.LoadFromFile(configPath);
        }

        class NullSteamApp : SteamApp
        {
            public override bool IsValid => false;
        }
    }

    public class ParseException : Exception
    {
        public ParseException() {}
        public ParseException(string message) : base(message) {}
    }

    // Here starts SteamKit2's KeyValue implementation, only copied here because of Assembly incompatibility with .NET Core/.NETStandard
    // TODO: Remove once compatible assembly available
    internal static class StreamHelpers
    {
        static byte[] data = new byte[8];
        public static Int16 ReadInt16(this Stream stream) {
            stream.Read(data, 0, 2);

            return BitConverter.ToInt16(data, 0);
        }

        public static UInt16 ReadUInt16(this Stream stream) {
            stream.Read(data, 0, 2);

            return BitConverter.ToUInt16(data, 0);
        }

        public static Int32 ReadInt32(this Stream stream) {
            stream.Read(data, 0, 4);

            return BitConverter.ToInt32(data, 0);
        }

        public static UInt32 ReadUInt32(this Stream stream) {
            stream.Read(data, 0, 4);

            return BitConverter.ToUInt32(data, 0);
        }

        public static UInt64 ReadUInt64(this Stream stream) {
            stream.Read(data, 0, 8);

            return BitConverter.ToUInt64(data, 0);
        }

        public static float ReadFloat(this Stream stream) {
            stream.Read(data, 0, 4);

            return BitConverter.ToSingle(data, 0);
        }

        public static string ReadNullTermString(this Stream stream, Encoding encoding) {
            int characterSize = encoding.GetByteCount("e");

            using (MemoryStream ms = new MemoryStream()) {

                while (true) {
                    byte[] data = new byte[characterSize];
                    stream.Read(data, 0, characterSize);

                    if (encoding.GetString(data, 0, characterSize) == "\0") {
                        break;
                    }

                    ms.Write(data, 0, data.Length);
                }

                return encoding.GetString(ms.ToArray());
            }
        }

        public static void WriteNullTermString(this Stream stream, string value, Encoding encoding) {
            var dataLength = encoding.GetByteCount(value);
            var data = new byte[dataLength + 1];
            encoding.GetBytes(value, 0, value.Length, data, 0);
            data[dataLength] = 0x00; // '\0'

            stream.Write(data, 0, data.Length);
        }

        static byte[] discardBuffer = new byte[2 << 12];

        public static void ReadAndDiscard(this Stream stream, int len) {
            while (len > discardBuffer.Length) {
                stream.Read(discardBuffer, 0, discardBuffer.Length);
                len -= discardBuffer.Length;
            }

            stream.Read(discardBuffer, 0, len);
        }
    }

    class KVTextReader : StreamReader
    {
        static Dictionary<char, char> escapedMapping = new Dictionary<char, char>
        {
            { 'n', '\n' },
            { 'r', '\r' },
            { 't', '\t' },
            // todo: any others?
        };

        public KVTextReader(KeyValue kv, Stream input)
            : base(input) {
            bool wasQuoted;
            bool wasConditional;

            KeyValue currentKey = kv;

            do {
                // bool bAccepted = true;

                string s = ReadToken(out wasQuoted, out wasConditional);

                if (string.IsNullOrEmpty(s))
                    break;

                if (currentKey == null) {
                    currentKey = new KeyValue(s);
                } else {
                    currentKey.Name = s;
                }

                s = ReadToken(out wasQuoted, out wasConditional);

                if (wasConditional) {
                    // bAccepted = ( s == "[$WIN32]" );

                    // Now get the '{'
                    s = ReadToken(out wasQuoted, out wasConditional);
                }

                if (s.StartsWith("{") && !wasQuoted) {
                    // header is valid so load the file
                    currentKey.RecursiveLoadFromBuffer(this);
                } else {
                    throw new Exception("LoadFromBuffer: missing {");
                }

                currentKey = null;
            }
            while (!EndOfStream);
        }

        private void EatWhiteSpace() {
            while (!EndOfStream) {
                if (!Char.IsWhiteSpace((char)Peek())) {
                    break;
                }

                Read();
            }
        }

        private bool EatCPPComment() {
            if (!EndOfStream) {
                char next = (char)Peek();
                if (next == '/') {
                    ReadLine();
                    return true;
                    /*
                     *  As came up in parsing the Dota 2 units.txt file, the reference (Valve) implementation
                     *  of the KV format considers a single forward slash to be sufficient to comment out the
                     *  entirety of a line. While they still _tend_ to use two, it's not required, and likely
                     *  is just done out of habit.
                     */
                }

                return false;
            }

            return false;
        }

        public string ReadToken(out bool wasQuoted, out bool wasConditional) {
            wasQuoted = false;
            wasConditional = false;

            while (true) {
                EatWhiteSpace();

                if (EndOfStream) {
                    return null;
                }

                if (!EatCPPComment()) {
                    break;
                }
            }

            if (EndOfStream)
                return null;

            char next = (char)Peek();
            if (next == '"') {
                wasQuoted = true;

                // "
                Read();

                var sb = new StringBuilder();
                while (!EndOfStream) {
                    if (Peek() == '\\') {
                        Read();

                        char escapedChar = (char)Read();
                        char replacedChar;

                        if (escapedMapping.TryGetValue(escapedChar, out replacedChar))
                            sb.Append(replacedChar);
                        else
                            sb.Append(escapedChar);

                        continue;
                    }

                    if (Peek() == '"')
                        break;

                    sb.Append((char)Read());
                }

                // "
                Read();

                return sb.ToString();
            }

            if (next == '{' || next == '}') {
                Read();
                return next.ToString();
            }

            bool bConditionalStart = false;
            int count = 0;
            var ret = new StringBuilder();
            while (!EndOfStream) {
                next = (char)Peek();

                if (next == '"' || next == '{' || next == '}')
                    break;

                if (next == '[')
                    bConditionalStart = true;

                if (next == ']' && bConditionalStart)
                    wasConditional = true;

                if (Char.IsWhiteSpace(next))
                    break;

                if (count < 1023) {
                    ret.Append(next);
                } else {
                    throw new Exception("ReadToken overflow");
                }

                Read();
            }

            return ret.ToString();
        }
    }

    /// <summary>
    /// Represents a recursive string key to arbitrary value container.
    /// </summary>
    public class KeyValue
    {
        enum Type : byte
        {
            None = 0,
            String = 1,
            Int32 = 2,
            Float32 = 3,
            Pointer = 4,
            WideString = 5,
            Color = 6,
            UInt64 = 7,
            End = 8,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValue"/> class.
        /// </summary>
        /// <param name="name">The optional name of the root key.</param>
        /// <param name="value">The optional value assigned to the root key.</param>
        public KeyValue(string name = null, string value = null) {
            this.Name = name;
            this.Value = value;

            Children = new List<KeyValue>();
        }

        /// <summary>
        /// Represents an invalid <see cref="KeyValue"/> given when a searched for child does not exist.
        /// </summary>
        public readonly static KeyValue Invalid = new KeyValue();

        /// <summary>
        /// Gets or sets the name of this instance.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the value of this instance.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets the children of this instance.
        /// </summary>
        public List<KeyValue> Children { get; private set; }


        /// <summary>
        /// Gets or sets the child <see cref="KeyValue" /> with the specified key.
        /// When retrieving by key, if no child with the given key exists, <see cref="Invalid" /> is returned.
        /// </summary>
        public KeyValue this[string key]
        {
            get
            {
                var child = this.Children
                    .FirstOrDefault(c => string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase));

                if (child == null) {
                    return Invalid;
                }

                return child;
            }
            set
            {
                var existingChild = this.Children
                    .FirstOrDefault(c => string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase));

                if (existingChild != null) {
                    // if the key already exists, remove the old one
                    this.Children.Remove(existingChild);
                }

                // ensure the given KV actually has the correct key assigned
                value.Name = key;

                this.Children.Add(value);
            }
        }

        /// <summary>
        /// Returns the value of this instance as a string.
        /// </summary>
        /// <returns>The value of this instance as a string.</returns>
        public string AsString() {
            return this.Value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as an unsigned byte.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as an unsigned byte.</returns>
        public byte AsUnsignedByte(byte defaultValue = default(byte)) {
            byte value;

            if (byte.TryParse(this.Value, out value) == false) {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as an unsigned short.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as an unsigned short.</returns>
        public ushort AsUnsignedShort(ushort defaultValue = default(ushort)) {
            ushort value;

            if (ushort.TryParse(this.Value, out value) == false) {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as an integer.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as an integer.</returns>
        public int AsInteger(int defaultValue = default(int)) {
            int value;

            if (int.TryParse(this.Value, out value) == false) {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as an unsigned integer.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as an unsigned integer.</returns>
        public uint AsUnsignedInteger(uint defaultValue = default(uint)) {
            uint value;

            if (uint.TryParse(this.Value, out value) == false) {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as a long.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as a long.</returns>
        public long AsLong(long defaultValue = default(long)) {
            long value;

            if (long.TryParse(this.Value, out value) == false) {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as an unsigned long.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as an unsigned long.</returns>
        public ulong AsUnsignedLong(ulong defaultValue = default(ulong)) {
            ulong value;

            if (ulong.TryParse(this.Value, out value) == false) {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as a float.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as a float.</returns>
        public float AsFloat(float defaultValue = default(float)) {
            float value;

            if (float.TryParse(this.Value, out value) == false) {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as a boolean.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as a boolean.</returns>
        public bool AsBoolean(bool defaultValue = default(bool)) {
            int value;

            if (int.TryParse(this.Value, out value) == false) {
                return defaultValue;
            }

            return value != 0;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as an enum.
        /// If the conversion is invalid, the default value is returned.
        /// </summary>
        /// <param name="defaultValue">The default value to return if the conversion is invalid.</param>
        /// <returns>The value of this instance as an enum.</returns>
        public T AsEnum<T>(T defaultValue = default(T))
            where T : struct {
            T value;

            if (Enum.TryParse<T>(this.Value, out value) == false) {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public override string ToString() {
            return string.Format("{0} = {1}", this.Name, this.Value);
        }

        /// <summary>
        /// Attempts to load the given filename as a text <see cref="KeyValue"/>.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <returns>a <see cref="KeyValue"/> instance if the load was successful, or <c>null</c> on failure.</returns>
        /// <remarks>
        /// This method will swallow any exceptions that occur when reading, use <see cref="ReadAsText"/> if you wish to handle exceptions.
        /// </remarks>
        public static KeyValue LoadAsText(string path) {
            return LoadFromFile(path, false);
        }

        /// <summary>
        /// Attempts to load the given filename as a binary <see cref="KeyValue"/>.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <returns>a <see cref="KeyValue"/> instance if the load was successful, or <c>null</c> on failure.</returns>
        [Obsolete("Use TryReadAsBinary instead. Note that TryLoadAsBinary returns the root object, not a dummy parent node containg the root object.")]
        public static KeyValue LoadAsBinary(string path) {
            var kv = LoadFromFile(path, true);
            if (kv == null) {
                return null;
            }

            var parent = new KeyValue();
            parent.Children.Add(kv);
            return parent;
        }

        /// <summary>
        /// Attempts to load the given filename as a binary <see cref="KeyValue"/>.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <param name="keyValue">The resulting <see cref="KeyValue"/> object if the load was successful, or <c>null</c> if unsuccessful.</param>
        /// <returns><c>true</c> if the load was successful, or <c>false</c> on failure.</returns>
        public static bool TryLoadAsBinary(string path, out KeyValue keyValue) {
            keyValue = LoadFromFile(path, true);
            return keyValue != null;
        }


        static KeyValue LoadFromFile(string path, bool asBinary) {
            if (File.Exists(path) == false) {
                return null;
            }

            try {
                using (var input = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    var kv = new KeyValue();

                    if (asBinary) {
                        if (kv.TryReadAsBinary(input) == false) {
                            return null;
                        }
                    } else {
                        if (kv.ReadAsText(input) == false) {
                            return null;
                        }
                    }

                    return kv;
                }
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// Attempts to create an instance of <see cref="KeyValue"/> from the given input text.
        /// </summary>
        /// <param name="input">The input text to load.</param>
        /// <returns>a <see cref="KeyValue"/> instance if the load was successful, or <c>null</c> on failure.</returns>
        /// <remarks>
        /// This method will swallow any exceptions that occur when reading, use <see cref="ReadAsText"/> if you wish to handle exceptions.
        /// </remarks>
        public static KeyValue LoadFromString(string input) {
            byte[] bytes = Encoding.UTF8.GetBytes(input);

            using (MemoryStream stream = new MemoryStream(bytes)) {
                var kv = new KeyValue();

                try {
                    if (kv.ReadAsText(stream) == false)
                        return null;

                    return kv;
                } catch (Exception) {
                    return null;
                }
            }
        }

        /// <summary>
        /// Populate this instance from the given <see cref="System.IO.Stream"/> as a text <see cref="KeyValue"/>.
        /// </summary>
        /// <param name="input">The input <see cref="System.IO.Stream"/> to read from.</param>
        /// <returns><c>true</c> if the read was successful; otherwise, <c>false</c>.</returns>
        public bool ReadAsText(Stream input) {
            this.Children = new List<KeyValue>();

            new KVTextReader(this, input);

            return true;
        }

        /// <summary>
        /// Opens and reads the given filename as text.
        /// </summary>
        /// <seealso cref="ReadAsText"/>
        /// <param name="filename">The file to open and read.</param>
        /// <returns><c>true</c> if the read was successful; otherwise, <c>false</c>.</returns>
        public bool ReadFileAsText(string filename) {
            using (FileStream fs = new FileStream(filename, FileMode.Open)) {
                return ReadAsText(fs);
            }
        }

        internal void RecursiveLoadFromBuffer(KVTextReader kvr) {
            bool wasQuoted;
            bool wasConditional;

            while (true) {
                // bool bAccepted = true;

                // get the key name
                string name = kvr.ReadToken(out wasQuoted, out wasConditional);

                if (string.IsNullOrEmpty(name)) {
                    throw new Exception("RecursiveLoadFromBuffer: got EOF or empty keyname");
                }

                if (name.StartsWith("}") && !wasQuoted)	// top level closed, stop reading
                    break;

                KeyValue dat = new KeyValue(name);
                dat.Children = new List<KeyValue>();
                this.Children.Add(dat);

                // get the value
                string value = kvr.ReadToken(out wasQuoted, out wasConditional);

                if (wasConditional && value != null) {
                    // bAccepted = ( value == "[$WIN32]" );
                    value = kvr.ReadToken(out wasQuoted, out wasConditional);
                }

                if (value == null)
                    throw new Exception("RecursiveLoadFromBuffer:  got NULL key");

                if (value.StartsWith("}") && !wasQuoted)
                    throw new Exception("RecursiveLoadFromBuffer:  got } in key");

                if (value.StartsWith("{") && !wasQuoted) {
                    dat.RecursiveLoadFromBuffer(kvr);
                } else {
                    if (wasConditional) {
                        throw new Exception("RecursiveLoadFromBuffer:  got conditional between key and value");
                    }

                    dat.Value = value;
                    // blahconditionalsdontcare
                }
            }
        }

        /// <summary>
        /// Saves this instance to file.
        /// </summary>
        /// <param name="path">The file path to save to.</param>
        /// <param name="asBinary">If set to <c>true</c>, saves this instance as binary.</param>
        public void SaveToFile(string path, bool asBinary) {
            using (var f = File.Create(path)) {
                SaveToStream(f, asBinary);
            }
        }

        /// <summary>
        /// Saves this instance to a given <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="System.IO.Stream"/> to save to.</param>
        /// <param name="asBinary">If set to <c>true</c>, saves this instance as binary.</param>
        public void SaveToStream(Stream stream, bool asBinary) {
            if (asBinary) {
                RecursiveSaveBinaryToStream(stream);
            } else {
                RecursiveSaveTextToFile(stream);
            }
        }

        void RecursiveSaveBinaryToStream(Stream f) {
            RecursiveSaveBinaryToStreamCore(f);
            f.WriteByte((byte)Type.End);
        }

        void RecursiveSaveBinaryToStreamCore(Stream f) {
            // Only supported types ATM:
            // 1. KeyValue with children (no value itself)
            // 2. String KeyValue
            if (Children.Any()) {
                f.WriteByte((byte)Type.None);
                f.WriteNullTermString(Name, Encoding.UTF8);
                foreach (var child in Children) {
                    child.RecursiveSaveBinaryToStreamCore(f);
                }
                f.WriteByte((byte)Type.End);
            } else {
                f.WriteByte((byte)Type.String);
                f.WriteNullTermString(Name, Encoding.UTF8);
                f.WriteNullTermString(Value ?? string.Empty, Encoding.UTF8);
            }
        }

        private void RecursiveSaveTextToFile(Stream stream, int indentLevel = 0) {
            // write header
            WriteIndents(stream, indentLevel);
            WriteString(stream, Name, true);
            WriteString(stream, "\n");
            WriteIndents(stream, indentLevel);
            WriteString(stream, "{\n");

            // loop through all our keys writing them to disk
            foreach (KeyValue child in Children) {
                if (child.Value == null) {
                    child.RecursiveSaveTextToFile(stream, indentLevel + 1);
                } else {
                    WriteIndents(stream, indentLevel + 1);
                    WriteString(stream, child.Name, true);
                    WriteString(stream, "\t\t");
                    WriteString(stream, child.AsString(), true);
                    WriteString(stream, "\n");
                }
            }

            WriteIndents(stream, indentLevel);
            WriteString(stream, "}\n");
        }

        void WriteIndents(Stream stream, int indentLevel) {
            WriteString(stream, new string('\t', indentLevel));
        }

        static void WriteString(Stream stream, string str, bool quote = false) {
            byte[] bytes = Encoding.UTF8.GetBytes((quote ? "\"" : "") + str.Replace("\"", "\\\"") + (quote ? "\"" : ""));
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Populate this instance from the given <see cref="System.IO.Stream"/> as a binary <see cref="KeyValue"/>.
        /// </summary>
        /// <param name="input">The input <see cref="System.IO.Stream"/> to read from.</param>
        /// <returns><c>true</c> if the read was successful; otherwise, <c>false</c>.</returns>
        [Obsolete("Use TryReadAsBinary instead. Note that TryReadAsBinary returns the root object, not a dummy parent node containg the root object.")]
        public bool ReadAsBinary(Stream input) {
            var dummyChild = new KeyValue();
            this.Children.Add(dummyChild);
            return dummyChild.TryReadAsBinary(input);
        }

        /// <summary>
        /// Populate this instance from the given <see cref="System.IO.Stream"/> as a binary <see cref="KeyValue"/>.
        /// </summary>
        /// <param name="input">The input <see cref="System.IO.Stream"/> to read from.</param>
        /// <returns><c>true</c> if the read was successful; otherwise, <c>false</c>.</returns>
        public bool TryReadAsBinary(Stream input) {
            return TryReadAsBinaryCore(input, this, null);
        }

        static bool TryReadAsBinaryCore(Stream input, KeyValue current, KeyValue parent) {
            current.Children = new List<KeyValue>();

            while (true) {
                var type = (Type)input.ReadByte();

                if (type == Type.End) {
                    break;
                }

                current.Name = input.ReadNullTermString(Encoding.UTF8);

                switch (type) {
                    case Type.None: {
                            var child = new KeyValue();
                            var didReadChild = TryReadAsBinaryCore(input, child, current);
                            if (!didReadChild) {
                                return false;
                            }
                            break;
                        }

                    case Type.String: {
                            current.Value = input.ReadNullTermString(Encoding.UTF8);
                            break;
                        }

                    case Type.WideString: {
                            MainLog.Logger.Warn("KeyValue: Encountered WideString type when parsing binary KeyValue, which is unsupported. Returning false.");
                            return false;
                        }

                    case Type.Int32:
                    case Type.Color:
                    case Type.Pointer: {
                            current.Value = Convert.ToString(input.ReadInt32());
                            break;
                        }

                    case Type.UInt64: {
                            current.Value = Convert.ToString(input.ReadUInt64());
                            break;
                        }

                    case Type.Float32: {
                            current.Value = Convert.ToString(input.ReadFloat());
                            break;
                        }

                    default: {
                            return false;
                        }
                }

                if (parent != null) {
                    parent.Children.Add(current);
                }
                current = new KeyValue();
            }

            return true;
        }
    }
}