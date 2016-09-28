// <copyright company="SIX Networks GmbH" file="UrlHelpers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SN.withSIX.Steam.Api.SteamKit.Utils
{
    public static class UrlHelpers
    {
        public static string ToSteamQueryString(this object @object) {
            if (@object == null)
                throw new ArgumentNullException("object");

            // Get all properties on the object
            var properties = @object.GetType().GetProperties()
                .Where(x => x.CanRead)
                .Where(x => x.GetValue(@object, null) != null)
                .ToDictionary(x => x.Name, x => x.GetValue(@object, null));

            SetSteamQueryItems(properties, x => !(x.Value is string) && x.Value is IEnumerable, SetSteamQueryEnumerable);
            SetSteamQueryItems(properties, x => !(x.Value is string) && x.Value is bool, (props, key) => {
                var val = (bool) props[key];
                props[key] = val ? 1 : 0;
            });
            SetSteamQueryItems(properties, x => !(x.Value is string) && x.Value is bool?, SetSteamQueryBoolean);
            SetSteamQueryItems(properties, x => !(x.Value is string) && x.Value is EPublishedFileQueryType,
                (props, key) => {
                    var val = (EPublishedFileQueryType) props[key];
                    props[key] = (int) val;
                });
            SetSteamQueryItems(properties, x => !(x.Value is string) && x.Value is EResult, (props, key) => {
                var val = (EResult) props[key];
                props[key] = (int) val;
            });
            //EPublishedFileQueryType
            // Concat all key/value pairs into a string separated by ampersand
            return string.Join("&", properties
                .Select(x => string.Concat(
                    x.Key, "=",
                    Uri.EscapeDataString(x.Value.ToString()))));
        }

        static void SetSteamQueryBoolean(Dictionary<string, object> properties, string key) {
            var val = (bool?) properties[key];
            if (!val.HasValue)
                properties.Remove(key);
            else
                properties[key] = val.Value ? 1 : 0;
        }

        static void SetSteamQueryItems(Dictionary<string, object> properties,
            Func<KeyValuePair<string, object>, bool> where, Action<Dictionary<string, object>, string> keyAction) {
            properties
                .Where(where)
                .Select(x => x.Key)
                .ToList().ForEach(x => keyAction(properties, x));
        }

        static void SetSteamQueryEnumerable(Dictionary<string, object> properties, string key) {
            var valueType = properties[key].GetType();
            var valueElemType = valueType.IsGenericType
                ? valueType.GetGenericArguments()[0]
                : valueType.GetElementType();
            if (valueElemType.IsPrimitive || (valueElemType == typeof(string))) {
                var enumerable = properties[key] as IEnumerable;
                properties.Remove(key);

                var i = 0;
                foreach (var item in enumerable) {
                    properties.Add(key + "[" + i + "]", item);
                    i++;
                }
            }
        }

        public static Uri AddToQueryString(this Uri input, string key, string value, bool escape = false) {
            var uriBuilder = new UriBuilder(input);
            var query = input.DecodeQueryParameters();
            query[key] = escape ? Uri.EscapeDataString(value) : value;

            uriBuilder.Query = query.ToQueryString();
            return uriBuilder.Uri;
        }

        public static Dictionary<string, string> DecodeQueryParameters(this Uri uri) {
            if (uri == null)
                throw new ArgumentNullException("uri");

            if (uri.Query.Length == 0)
                return new Dictionary<string, string>();

            return uri.Query.TrimStart('?')
                .Split(new[] {'&', ';'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(kvp => kvp.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(kvp => HttpUtility.UrlDecode(kvp[0]),
                    kvp =>
                        HttpUtility.UrlDecode(kvp.Length > 2
                            ? string.Join("=", kvp, 1, kvp.Length - 1)
                            : (kvp.Length > 1 ? kvp[1] : "")));
        }

        internal static string ToQueryString(this Dictionary<string, string> query) {
            var sb = new StringBuilder();
            foreach (var item in query) {
                if (sb.Length > 0)
                    sb.Append('&');

                sb.Append(HttpUtility.UrlEncode(item.Key));
                if (item.Value == null)
                    continue;
                sb.Append("=");
                sb.Append(HttpUtility.UrlEncode(item.Value));
            }
            return sb.ToString();
        }
    }
}