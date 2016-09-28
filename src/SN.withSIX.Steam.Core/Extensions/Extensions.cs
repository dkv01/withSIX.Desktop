using System;
using System.Collections.Generic;
using System.Linq;

namespace SN.withSIX.Steam.Core.Extensions
{
    public static class Extensions
    {
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0,
            DateTimeKind.Utc);

        public static DateTime UnixTimeToDateTime(this string text) {
            var seconds = double.Parse(text);
            return Epoch.AddSeconds(seconds);
        }

        /// <summary>
        ///     Converts a given DateTime into a Unix timestamp
        /// </summary>
        /// <param name="value">Any DateTime</param>
        /// <returns>The given DateTime in Unix timestamp format</returns>
        public static int ToUnixTimestamp(this DateTime value)
            => (int) Math.Truncate(value.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds);

        public static KeyValue GetKeyValue(this KeyValue This, IEnumerable<string> pars)
            => pars.Aggregate(This, (current, p) => {
                var newC = current[p];
                if (newC == KeyValue.Invalid)
                    throw new KeyNotFoundException($"{p} key is not found");
                return newC;
            });

        public static KeyValue GetKeyValue(this KeyValue This, params string[] pars)
            => GetKeyValue(This, (IEnumerable<string>) pars);

        public static bool ContainsKey(this KeyValue This, string key) => This[key] != KeyValue.Invalid;

        public static void Remove(this KeyValue This, string key) => This.Children.RemoveAll(x => x.Name == key);
    }
}