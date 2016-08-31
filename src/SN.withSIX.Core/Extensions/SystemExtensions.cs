// <copyright company="SIX Networks GmbH" file="SystemExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MoreLinq;
using NDepend.Path;
using withSIX.Api.Models;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.Core.Extensions
{
    public static class TaskExtExt
    {
        public static Task<T> StartLongRunningTask<T>(Func<Task<T>> fnc) => Task.Factory.StartNew(fnc, TaskCreationOptions.LongRunning).Unwrap();
        public static Task StartLongRunningTask(Func<Task> fnc) => Task.Factory.StartNew(fnc, TaskCreationOptions.LongRunning).Unwrap();
        public static Task StartLongRunningTask(Action fnc) => Task.Factory.StartNew(fnc, TaskCreationOptions.LongRunning);
        public static Task<T> StartLongRunningTask<T>(Func<T> fnc) => Task.Factory.StartNew(fnc, TaskCreationOptions.LongRunning);

        public static Task<T> StartLongRunningTask<T>(Func<Task<T>> fnc, CancellationToken token) => Task.Factory.StartNew(fnc, token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        public static Task StartLongRunningTask(Func<Task> fnc, CancellationToken token) => Task.Factory.StartNew(fnc, token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        public static Task StartLongRunningTask(Action fnc, CancellationToken token) => Task.Factory.StartNew(fnc, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        public static Task<T> StartLongRunningTask<T>(Func<T> fnc, CancellationToken token) => Task.Factory.StartNew(fnc, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public static class SystemExtensions
    {
        static readonly Regex singleVersion = new Regex(@"^\d+$", RegexOptions.Compiled);
        static readonly Regex doubleVersion = new Regex(@"^\d+\.\d+$", RegexOptions.Compiled);
        static readonly DateTime unixBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static ShortGuid ToShortId(this Guid id) => new ShortGuid(id);

        [Obsolete("Find better approach, as this will leave resources when not cancelled")]
        public static Task ThrowWhenCanceled(this CancellationToken cancellationToken)
            => Task.Delay(-1, cancellationToken);

        public static void DoWith<T>(this T This, Action<T> exp) => exp(This);

        public static void DoWith<T, T2>(this T This, Func<T, T2> exp) => exp(This);

        public static Process Launch(this ProcessStartInfo processStartInfo) {
            MainLog.Logger.Info("Launching {0} from {1} with {2}", processStartInfo.FileName,
                processStartInfo.WorkingDirectory, processStartInfo.Arguments);
            return Process.Start(processStartInfo);
        }

        public static Lazy<T> CreateLazy<T>(this Func<T> creator) => new Lazy<T>(creator);

        public static byte[] Combine(this byte[] @this, params byte[][] arrays) {
            var rv = new byte[arrays.Sum(a => a.Length) + @this.Length];
            var offset = 0;

            Buffer.BlockCopy(@this, 0, rv, offset, @this.Length);
            offset += @this.Length;

            foreach (var array in arrays) {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        public static IEnumerable<T> AsEnumerable<T>(this T enumVal) where T : struct, IConvertible => AsEnumerable<T>();
        public static IEnumerable<T> AsEnumerable<T>() => Enum.GetValues(typeof(T)).Cast<T>();

        public static void TryKill(this Process p) {
            var id = -1;
            try {
                p.Kill();
            } catch (Win32Exception e) {
                try {
                    id = p.Id;
                } catch { }
                MainLog.Logger.FormattedWarnException(e, "Error while trying to kill process " + id);
            } catch (InvalidOperationException e) {
                try {
                    id = p.Id;
                } catch { }
                MainLog.Logger.FormattedWarnException(e, "Error while trying to kill process" + id);
            }
        }

        public static IEnumerable<IAbsoluteFilePath> GetFiles(this IDirectoryPath path,
            string searchPatternExpression = "",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => GetFiles(path.ToString(), searchPatternExpression, searchOption).Select(x => x.ToAbsoluteFilePath());

        public static IEnumerable<IAbsoluteFilePath> GetFiles(this IDirectoryPath path,
            IEnumerable<string> searchPatterns,
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => GetFiles(path.ToString(), searchPatterns, searchOption).Select(x => x.ToAbsoluteFilePath());

        public static T GetMetaData<T>(this object t, T def = default(T)) where T : Attribute
            => t.GetType().GetMetaData(def);

        public static T GetMetaData<T>(this Type t, T def = default(T)) where T : Attribute
            => t.GetCustomAttribute<T>() ?? def;

        public static IEnumerable<string> GetFiles(this DirectoryInfo path,
            string searchPatternExpression = "",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => GetFiles(path.ToString(), searchPatternExpression, searchOption);

        public static IEnumerable<string> GetFiles(this DirectoryInfo path,
            IEnumerable<string> searchPatterns,
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => GetFiles(path.ToString(), searchPatterns, searchOption);

        // Regex version
        public static IEnumerable<string> GetFiles(string path,
            string searchPatternExpression = "",
            SearchOption searchOption = SearchOption.TopDirectoryOnly) {
            var reSearchPattern = new Regex(searchPatternExpression);
            return Directory.EnumerateFiles(path, "*", searchOption)
                .Where(file =>
                    reSearchPattern.IsMatch(Path.GetExtension(file)));
        }

        // Takes same patterns, and executes in parallel
        public static IEnumerable<string> GetFiles(string path,
            IEnumerable<string> searchPatterns,
            SearchOption searchOption = SearchOption.TopDirectoryOnly) => searchPatterns.AsParallel()
                .SelectMany(searchPattern =>
                    Directory.EnumerateFiles(path, searchPattern, searchOption));

        public static Uri GetParentUri(this string url) => GetParentUri(new Uri(url));

        public static double CalculateProgress(this int completed, int count, double progress) {
            if (completed < 0)
                throw new ArgumentOutOfRangeException(nameof(completed), "below 0");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "below 0");
            if (count == 0)
                return 0;
            return (progress/100 + completed)/count*100;
        }

        public static double CalculateProgress(this int completed, int count) {
            if (completed < 0)
                throw new ArgumentOutOfRangeException(nameof(completed), "below 0");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "below 0");
            if (count == 0)
                return 0;
            return completed/(double) count*100;
        }

        public static Uri GetParentUri(this Uri uri) {
            var parts = uri.GetLeftPart(UriPartial.Path).Split('/');
            return new Uri(string.Join("/", parts.Take(parts.Length - 1)));
        }

        public static string ToBase64(this string inputString)
            => Convert.ToBase64String(Encoding.UTF8.GetBytes(inputString));

        public static Exception FindInnerException<T>(this Exception e) {
            if (e is T)
                return e;

            while (e.InnerException != null) {
                e = e.InnerException;
                if (e is T)
                    return e;
            }

            return null;
        }

        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> defaults, IEnumerable<KeyValuePair<TKey, TValue>> overrides) {
            Contract.Requires<ArgumentNullException>(overrides != null);
            return overrides.Concat(defaults).DistinctBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        public static IDictionary<TKey, TValue> MergeIfOverrides<TKey, TValue>(
            this IDictionary<TKey, TValue> defaults, IEnumerable<KeyValuePair<TKey, TValue>> overrides)
            => overrides == null
                ? defaults
                : overrides.Concat(defaults).DistinctBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

        public static string OrEmpty(this string str) => str ?? string.Empty;

        public static IDictionary<string, object> Inspect(this object obj) => InspectPublicProperties(obj)
            .Concat(InspectPublicFields(obj))
            .DistinctBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);

        static Dictionary<string, object> InspectPublicProperties(object obj)
            => obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.CanRead)
                .ToDictionary(x => x.Name, x => x.GetValue(obj));

        static Dictionary<string, object> InspectPublicFields(object obj) => obj.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(x => x.Name, x => x.GetValue(obj));

        public static bool TryBool(this string boolAsString) {
            bool b;
            return boolAsString != null && bool.TryParse(boolAsString.ToLower(), out b) && b;
        }

        public static byte[] ToBytes(this Stream stream) {
            var originalPosition = stream.Position;
            stream.Position = 0;

            try {
                var readBuffer = new byte[4096];

                var totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0) {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length) {
                        var nextByte = stream.ReadByte();
                        if (nextByte != -1) {
                            var temp = new byte[readBuffer.Length*2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte) nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                var buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead) {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            } finally {
                stream.Position = originalPosition;
            }
        }

        public static bool CheckParamCount(this IEnumerable list, int min) {
            Contract.Requires<ArgumentNullException>(list != null);
            return list.Cast<object>().Count() >= min;
        }

        public static void ConfirmParamCount(this IEnumerable list, int min) {
            Contract.Requires<ArgumentNullException>(list != null);
            if (!CheckParamCount(list, min))
                throw new ArgumentException($"Wrong number of parameters. {list.Cast<object>().Count()} vs {min}");
        }

        public static Version BumpVersion(this Version version) {
            if (version.Revision > -1)
                return new Version(version.Major, version.Minor, version.Build, version.Revision + 1);
            return new Version(version.Major, version.Minor, version.Build + 1);
        }

        public static Version TryParseVersion(this string versionAsString) {
            if (versionAsString == null)
                return null;
            Version version;
            return Version.TryParse(versionAsString, out version) ? version : null;
        }

        public static Version ParseVersion(this string ver) {
            Version version;
            if (string.IsNullOrWhiteSpace(ver) ||
                !Version.TryParse(ver.Replace(" ", string.Empty).Replace(",", "."), out version))
                return null;
            return !string.IsNullOrWhiteSpace(version.ToString()) ? version : null;
        }

        public static Version ToVersion(this string versionString) {
            Version version;
            Version.TryParse(versionString, out version);
            return version;
        }

        public static Version ToNormalizedVersion(this string versionString) {
            Contract.Requires<ArgumentNullException>(versionString != null);
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(versionString));

            if (singleVersion.IsMatch(versionString))
                versionString += ".0";
            if (doubleVersion.IsMatch(versionString))
                versionString += ".0";
            return ToVersion(versionString);
        }

        public static Version BumpVersion(this string version) => new Version(version).BumpVersion();

        public static string EncodeTo64(this string toEncode) {
            var toEncodeAsBytes
                = Encoding.UTF8.GetBytes(toEncode);
            var returnValue
                = Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        public static string Decode64(this string toDecode) {
            var toDecodedAsBytes
                = Convert.FromBase64String(toDecode);
            return
                Encoding.UTF8.GetString(toDecodedAsBytes);
        }

        public static string UppercaseFirst(this string s) {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            return char.ToUpper(s[0]) + s.Substring(1).ToLower();
        }

        public static string FormatWith(this string format, params object[] args) {
            Contract.Requires<ArgumentNullException>(format != null);

            return string.Format(format, args);
        }

        public static string FormatWith(this string format, IFormatProvider provider, params object[] args) {
            Contract.Requires<ArgumentNullException>(format != null);

            return string.Format(provider, format, args);
        }

        public static bool IsBlank(this string str) => string.IsNullOrEmpty(str);

        public static bool IsBlankOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

        public static bool Like(this string s, string match, bool caseInsensitive = true) {
            //Nothing matches a null mask or null input string
            if (match == null || s == null)
                return false;
            //Null strings are treated as empty and get checked against the mask.
            //If checking is case-insensitive we convert to uppercase to facilitate this.
            if (caseInsensitive) {
                s = s.ToUpperInvariant();
                match = match.ToUpperInvariant();
            }
            //Keeps track of our position in the primary string - s.
            var j = 0;
            //Used to keep track of multi-character wildcards.
            var matchanymulti = false;
            //Used to keep track of multiple possibility character masks.
            string multicharmask = null;
            var inversemulticharmask = false;
            for (var i = 0; i < match.Length; i++) {
                //If this is the last character of the mask and its a % or * we are done
                if (i == match.Length - 1 && (match[i] == '%' || match[i] == '*'))
                    return true;
                //A direct character match allows us to proceed.
                var charcheck = true;
                //Backslash acts as an escape character.  If we encounter it, proceed
                //to the next character.
                if (match[i] == '\\') {
                    i++;
                    if (i == match.Length)
                        i--;
                } else {
                    //If this is a wildcard mask we flag it and proceed with the next character
                    //in the mask.
                    if (match[i] == '%' || match[i] == '*') {
                        matchanymulti = true;
                        continue;
                    }
                    //If this is a single character wildcard advance one character.
                    if (match[i] == '_') {
                        //If there is no character to advance we did not find a match.
                        if (j == s.Length)
                            return false;
                        j++;
                        continue;
                    }
                    if (match[i] == '[') {
                        var endbracketidx = match.IndexOf(']', i);
                        //Get the characters to check for.
                        multicharmask = match.Substring(i + 1, endbracketidx - i - 1);
                        //Check for inversed masks
                        inversemulticharmask = multicharmask.StartsWith("^");
                        //Remove the inversed mask character
                        if (inversemulticharmask)
                            multicharmask = multicharmask.Remove(0, 1);
                        //Unescape \^ to ^
                        multicharmask = multicharmask.Replace("\\^", "^");

                        //Prevent direct character checking of the next mask character
                        //and advance to the next mask character.
                        charcheck = false;
                        i = endbracketidx;
                        //Detect and expand character ranges
                        if (multicharmask.Length == 3 && multicharmask[1] == '-') {
                            var newmask = "";
                            var first = multicharmask[0];
                            var last = multicharmask[2];
                            if (last < first) {
                                first = last;
                                last = multicharmask[0];
                            }
                            var c = first;
                            while (c <= last) {
                                newmask += c;
                                c++;
                            }
                            multicharmask = newmask;
                        }
                        //If the mask is invalid we cannot find a mask for it.
                        if (endbracketidx == -1)
                            return false;
                    }
                }
                //Keep track of match finding for this character of the mask.
                var matched = false;
                while (j < s.Length) {
                    //This character matches, move on.
                    if (charcheck && s[j] == match[i]) {
                        j++;
                        matched = true;
                        break;
                    }
                    //If we need to check for multiple charaters to do.
                    if (multicharmask != null) {
                        var ismatch = multicharmask.Contains(s[j]);
                        //If this was an inverted mask and we match fail the check for this string.
                        //If this was not an inverted mask check and we did not match fail for this string.
                        if (inversemulticharmask && ismatch ||
                            !inversemulticharmask && !ismatch) {
                            //If we have a wildcard preceding us we ignore this failure
                            //and continue checking.
                            if (matchanymulti) {
                                j++;
                                continue;
                            }
                            return false;
                        }
                        j++;
                        matched = true;
                        //Consumse our mask.
                        multicharmask = null;
                        break;
                    }
                    //We are in an multiple any-character mask, proceed to the next character.
                    if (matchanymulti) {
                        j++;
                        continue;
                    }
                    break;
                }
                //We've found a match - proceed.
                if (matched) {
                    matchanymulti = false;
                    continue;
                }

                //If no match our mask fails
                return false;
            }
            //Some characters are left - our mask check fails.
            if (j < s.Length)
                return false;
            //We've processed everything - this is a match.
            return true;
        }

        public static string ToUnderscore(this string str) {
            const string rgx = @"([A-Z]+)([A-Z][a-z])";
            const string rgx2 = @"([a-z\d])([A-Z])";

            return Regex.Replace(Regex.Replace(str, rgx, "$1_$2"), rgx2, "$1_$2").ToLower();
        }

        public static string Pluralize(this string str) {
            if (str.EndsWith("s"))
                return str;

            if (str.EndsWith("y"))
                return str.Substring(0, str.Length - 1) + "ies";

            return str + "s";
        }

        public static string PluralizeIfNeeded(this string str, int count) => count == 1 ? str : str.Pluralize();

        public static int TryInt(this string val) {
            int result;
            return int.TryParse(val, out result) ? result : 0;
        }

        public static uint TryUint(this string val) {
            uint result;
            return uint.TryParse(val, out result) ? result : 0;
        }

        public static string[] TrySplit(this string settings, char splitStr)
            => settings?.Split(splitStr) ?? new string[0];

        public static string SplitCamelCase(this string str) => Regex.Replace(
            Regex.Replace(
                str,
                @"(\P{Ll})(\P{Ll}\p{Ll})",
                "$1 $2"
                ),
            @"(\p{Ll})(\P{Ll})",
            "$1 $2"
            );

        public static Version TryVersion(this string val) {
            Version result;
            return Version.TryParse(val, out result) ? result : default(Version);
        }

        public static int? TryIntNullable(this string val) {
            int result;
            return int.TryParse(val, out result) ? (int?) result : null;
        }

        public static double TryDouble(this string val, NumberStyles numberStyle = NumberStyles.Float,
            CultureInfo culture = null) {
            if (culture == null)
                culture = CultureInfo.InvariantCulture;
            double result;
            return double.TryParse(val, numberStyle, culture, out result) ? result : 0;
        }

        public static long TryLong(this string val) {
            long result;
            return long.TryParse(val, out result) ? result : 0;
        }

        public static long? TryLongNullable(this string val) {
            long result;
            return long.TryParse(val, out result) ? (long?) result : null;
        }

        public static IList<T> ToList<T>(this IEnumerable<T> items, Action<T> action) {
            Contract.Requires<ArgumentNullException>(items != null);
            var list = items.ToList();
            foreach (var item in list)
                action(item);
            return list;
        }

        public static bool None<T>(this IEnumerable<T> items, Func<T, bool> predicate) {
            Contract.Requires<ArgumentNullException>(items != null);
            Contract.Requires<ArgumentNullException>(predicate != null);
            return !items.Any(predicate);
        }

        public static bool In(this string value, params string[] values)
            => !string.IsNullOrEmpty(value) && values.Any(s => value.Equals(s, StringComparison.OrdinalIgnoreCase));

        public static bool EndsWithAny(this string value, params string[] values) => !string.IsNullOrEmpty(value) &&
                                                                                     values.Any(
                                                                                         s =>
                                                                                             value.EndsWith(s,
                                                                                                 StringComparison
                                                                                                     .OrdinalIgnoreCase));

        public static bool Contains(this string source, string toCheck,
            StringComparison comp = StringComparison.CurrentCulture) => source.IndexOf(toCheck, comp) > -1;

        public static bool ContainsIgnoreCase(this string source, string toCheck)
            => source.Contains(toCheck, StringComparison.CurrentCultureIgnoreCase);

        public static bool ContainsIgnoreCase(this IEnumerable<string> source, string toCheck)
            => source.Contains(toCheck, StringComparer.InvariantCultureIgnoreCase);

        public static bool NullSafeContains(this IEnumerable<string> value, string toCheck,
            StringComparer comp = null) {
            if (value == null)
                return false;

            if (comp == null)
                comp = StringComparer.CurrentCulture;

            return toCheck != null && value.Contains(toCheck, comp);
        }

        public static bool NullSafeContainsIgnoreCase(this IEnumerable<string> value, string toCheck)
            => value.NullSafeContains(toCheck, StringComparer.CurrentCultureIgnoreCase);

        public static bool NullSafeContains(this string value, string toCheck,
            StringComparison comp = StringComparison.CurrentCulture) {
            if (string.IsNullOrEmpty(value))
                return false;

            return toCheck != null && value.Contains(toCheck, comp);
        }

        public static bool NullSafeContainsIgnoreCase(this string value, string toCheck)
            => value.NullSafeContains(toCheck, StringComparison.CurrentCultureIgnoreCase);

        public static int NullSafeIndexOfIgnoreCase(this string value, string toCheck) {
            if (value == null)
                return -1;
            return value.IndexOf(toCheck, StringComparison.InvariantCultureIgnoreCase);
        }

        public static int NullSafeIndex(this string value, string toCheck) {
            if (value == null)
                return -1;
            return value.IndexOf(toCheck);
        }

        public static string Dump<T>(this IEnumerable<T> val, string message = null) {
            var str = val.Stringify();
            return message == null ? str : $"{message}: [{str}]";
        }

        public static string Stringify<T>(this IEnumerable<T> val) {
            Contract.Requires<ArgumentNullException>(val != null);
            return string.Join(",", val);
        }

        public static Uri ToUri(this string url) => new Uri(url);

        public static string EscapedUri(this Uri uri) => Uri.EscapeUriString(uri.ToString());

        public static Uri AuthlessUri(this Uri uri)
            => new UriBuilder(uri.Scheme, uri.Host, uri.Port, uri.AbsolutePath, uri.Query).Uri;

        public static CachedEnumerable<T> Cache<T>(this IEnumerable<T> enumerable)
            => new CachedEnumerable<T>(enumerable);

        public static IEnumerable<string> SplitBySize(this string @this, int chunkSize)
            => Enumerable.Range(0, @this.Length/chunkSize)
                .Select(i => @this.Substring(i*chunkSize, chunkSize));
    }

    public class CachedEnumerable<T> : IEnumerable<T>
    {
        readonly IList<T> _cache = new List<T>();
        readonly IEnumerable<T> _originalEnumerable;
        IEnumerator<T> _originalEnumerator;

        public CachedEnumerable(IEnumerable<T> enumerable) {
            _originalEnumerable = enumerable;
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator() {
            foreach (var t in _cache)
                yield return t;
            if (_originalEnumerator == null)
                _originalEnumerator = _originalEnumerable.GetEnumerator();
            while (_originalEnumerator.MoveNext()) {
                _cache.Add(_originalEnumerator.Current);
                yield return _originalEnumerator.Current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}