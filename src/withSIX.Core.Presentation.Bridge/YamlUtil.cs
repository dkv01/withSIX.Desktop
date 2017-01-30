// <copyright company="SIX Networks GmbH" file="YamlUtil.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Sync.Core;
using withSIX.Sync.Core.Legacy;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace withSIX.Core.Presentation.Bridge
{
    // TODO: YamlDotNet is currently not PCL compatible
    // So we want to move this out to the highest layer, and DI it.
    public class YamlUtil : IYamlUtil, IPresentationService
    {
        internal static readonly Deserializer Deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(new CustomCamelCaseNamingConvention())
            .Build();
        private static readonly Serializer serializer = new SerializerBuilder()
            .WithNamingConvention(new CustomCamelCaseNamingConvention()).Build();

        [Obsolete("Use extensions")]
        public Task<T> GetYaml<T>(Uri uri, CancellationToken ct = default(CancellationToken), string token = null)
            => uri.GetYaml<T>(ct, token);

        public T NewFromYamlFile<T>(IAbsoluteFilePath fileName) => NewFromYaml<T>(fileName.ReadAllText());

        public T NewFromYaml<T>(string yaml) {
            try {
                return yaml.FromYaml<T>();
            } catch (Exception ex) {
                throw new YamlParseException(ex.Message, ex);
            }
        }

        public string ToYaml(object graph) {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            using (var text = new StringWriter()) {
                text.Write("--- \r\n");
                var emitter = new Emitter(text);
                serializer.Serialize(emitter, graph);
                return text.ToString();
            }
        }

        public void ToYamlFile(IBaseYaml graph, IAbsoluteFilePath fileName)
            => YamlIoExtensions._SaveYaml(graph.ToYaml(), fileName);

        // We call the graph ToYaml() because it manually serialises..

        public void PrintMapping(YamlMappingNode mapping) {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));

            foreach (var entry in mapping.Children) {
                var key = ((YamlScalarNode) entry.Key).Value;
                var value = string.Empty;

                try {
                    value = ((YamlScalarNode) entry.Value).Value;
                } catch (Exception) {}

                Console.WriteLine("{0}: {1}", key, value);
            }
        }

        public Dictionary<string, string> ToStringDictionary(IDictionary<YamlNode, YamlNode> list) {
            if (list == null) throw new ArgumentNullException(nameof(list));

            return list.ToDictionary(ent => ((YamlScalarNode) ent.Key).Value,
                ent => ((YamlScalarNode) ent.Value).Value);
        }

        public Dictionary<string, int> ToIntDictionary(IDictionary<YamlNode, YamlNode> list) {
            if (list == null) throw new ArgumentNullException(nameof(list));

            return list.ToDictionary(ent => ((YamlScalarNode) ent.Key).Value,
                ent => ((YamlScalarNode) ent.Value).Value.TryInt());
        }

        public Dictionary<string, long> ToLongDictionary(IDictionary<YamlNode, YamlNode> list) {
            if (list == null) throw new ArgumentNullException(nameof(list));

            return list.ToDictionary(ent => ((YamlScalarNode) ent.Key).Value,
                ent => ((YamlScalarNode) ent.Value).Value.TryLong());
        }

        public Dictionary<string, string> GetStringDictionaryInternal(YamlNode node) {
            if (node == null) throw new ArgumentNullException(nameof(node));

            var mapping = node as YamlMappingNode;
            if (mapping == null) {
                var mapping2 = node as YamlScalarNode;
                if ((mapping2 != null) && string.IsNullOrEmpty(mapping2.Value))
                    return null;

                var mapping3 = node as YamlSequenceNode;
                if (mapping3 != null) {
                    return mapping3
                        .Select(n => ((YamlSequenceNode) n).ToArray())
                        .ToDictionary(ar => ((YamlScalarNode) ar[0]).Value, ar => ((YamlScalarNode) ar[1]).Value);
                }

                throw new YamlExpectedOtherNodeTypeException("Expected YamlMappingNode");
            }
            return ToStringDictionary(mapping.Children);
        }

        public string[] GetStringArrayInternal(YamlNode node) {
            if (node == null) throw new ArgumentNullException(nameof(node));

            var mapping = node as YamlSequenceNode;
            if (mapping == null) {
                var mapping2 = node as YamlScalarNode;
                if ((mapping2 != null) && string.IsNullOrEmpty(mapping2.Value))
                    return null;
                throw new YamlExpectedOtherNodeTypeException("Expected YamlSequenceNode");
            }
            return mapping.Children.Select(x => ((YamlScalarNode) x).Value).ToArray();
        }

        YamlNode GetRootNode(YamlStream yaml) {
            if (yaml == null) throw new ArgumentNullException(nameof(yaml));

            if (yaml.Documents.Count == 0)
                throw new YamlParseException("Contains no documents");
            return yaml.Documents[0].RootNode;
        }

        public YamlMappingNode GetMapping(YamlStream yaml) {
            if (yaml == null) throw new ArgumentNullException(nameof(yaml));

            var node = GetRootNode(yaml);
            var mapped = node as YamlMappingNode;
            if (mapped == null)
                throw new YamlParseException("Not a mapping");
            return mapped;
        }

        public DateTime GetDateTimeOrDefault(YamlNode node) {
            if (node == null) throw new ArgumentNullException(nameof(node));

            var val = ((YamlScalarNode) node).Value;
            DateTime dt;
            DateTime.TryParse(val, out dt);
            return dt;
        }

        public string GetStringOrDefault(YamlNode node) {
            if (node == null) throw new ArgumentNullException(nameof(node));

            return ((YamlScalarNode) node).Value;
        }

        public long GetLongOrDefault(YamlNode node) {
            if (node == null) throw new ArgumentNullException(nameof(node));

            return ((YamlScalarNode) node).Value.TryLong();
        }

        public int GetIntOrDefault(YamlNode node) {
            if (node == null) throw new ArgumentNullException(nameof(node));

            return ((YamlScalarNode) node).Value.TryInt();
        }

        public bool GetBoolOrDefault(YamlNode node) {
            if (node == null) throw new ArgumentNullException(nameof(node));

            return ((YamlScalarNode) node).Value.TryBool();
        }

        public string[] GetStringArray(YamlNode node) => GetStringArrayInternal(node) ?? new string[0];

        public Dictionary<string, string> GetStringDictionary(YamlNode node)
            => GetStringDictionaryInternal(node) ?? new Dictionary<string, string>();


        public sealed class CustomCamelCaseNamingConvention : INamingConvention
        {
            public string Apply(string value) {
                value = value?.ToUnderscore();
                var s = (value != null) && !value.StartsWith(":") ? ":" + value : value;
                return s;
            }

            //readonly INamingConvention convention = new UnderscoredNamingConvention();
            //public string Apply(string value) {
            //var s = (value == null) || !value.StartsWith(":") ? value : value.Substring(1);
            //return convention.Apply(s);
            //}
        }
    }

    public static class YamlIoExtensions
    {
        internal static void _SaveYaml(string yaml, IAbsoluteFilePath fileName) {
            if (yaml == null) throw new ArgumentNullException(nameof(yaml));
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));

            Tools.FileTools.SafeIO.SafeSave(x => {
                using (var sr = new StreamWriter(x.ToString()))
                    sr.Write(yaml);
            }, fileName);
        }

        internal static YamlStream ReadYaml(this string yaml) {
            if (yaml == null) throw new ArgumentNullException(nameof(yaml));

            using (var input = new StringReader(yaml)) {
                var yaml1 = new YamlStream();
                yaml1.Load(input);
                return yaml1;
            }
        }

        internal static YamlStream ReadYamlFile(this IAbsoluteFilePath yaml) {
            if (yaml == null) throw new ArgumentNullException(nameof(yaml));

            using (var stream = File.Open(yaml.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var input = new StreamReader(stream)) {
                var yaml1 = new YamlStream();
                yaml1.Load(input);
                return yaml1;
            }
        }
    }

    public static class W6DownloaderYamlExtensions
    {
        public static Task<T> GetYaml<T>(this Uri uri, CancellationToken ct = default(CancellationToken),
                string token = null)
            => uri.GetYaml<T>(ct, client => W6DownloaderExtensions.Setup(client, uri, token));

        public static Task<string> GetYamlText(this Uri uri, CancellationToken ct = default(CancellationToken),
                string token = null)
            => uri.GetYamlText(ct, client => W6DownloaderExtensions.Setup(client, uri, token));

        public static Task<string> PostYaml(this object model, Uri uri,
                CancellationToken ct = default(CancellationToken), string token = null)
            => model.PostJson(uri, ct, client => W6DownloaderExtensions.Setup(client, uri, token));
    }

    public static class DownloaderYamlExtensions
    {
        private const string YamlMimeType = "text/yaml";
        private const string YamlMimeAcceptType =
            "text/html,application/xhtml+xml,application/xml,text/yaml,text/x-yaml,application/yaml,application/x-yaml";

        public static async Task<T> GetYaml<T>(this Uri uri, CancellationToken ct = default(CancellationToken),
            Action<HttpClient> setup = null) {
            var c = await uri.GetYamlText(ct, setup).ConfigureAwait(false);
            return c.FromYaml<T>();
        }

        public static T FromYaml<T>(this string c) {
            using (var stringReader = new StringReader(c))
                return YamlUtil.Deserializer.Deserialize<T>(stringReader);
        }

        public static async Task<string> GetYamlText(this Uri uri, CancellationToken ct = default(CancellationToken),
            Action<HttpClient> setup = null) {
            using (var client = DownloaderExtensions.GetHttpClient()) {
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(YamlMimeType));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", YamlMimeAcceptType);
                DownloaderExtensions.HandleSetup(setup, client, uri);

                using (var r = await client.GetAsync(uri, ct).ConfigureAwait(false)) {
                    await r.EnsureSuccessStatusCodeAsync().ConfigureAwait(false);
                    return await DownloaderExtensions.TryReadStringContent(r).ConfigureAwait(false);
                }
            }
        }

        public static async Task<string> PostYaml(object model, Uri uri,
            CancellationToken ct = default(CancellationToken), Action<HttpClient> setup = null) {
            DownloaderExtensions.Validator.ValidateObject(model);
            using (var client = new HttpClient()) {
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(YamlMimeType));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", YamlMimeAcceptType);
                DownloaderExtensions.HandleSetup(setup, client, uri);

                using (var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, YamlMimeType)) {
                    using (var r = await client.PostAsync(uri, content, ct).ConfigureAwait(false)) {
                        await r.EnsureSuccessStatusCodeAsync().ConfigureAwait(false);
                        return await DownloaderExtensions.TryReadStringContent(r).ConfigureAwait(false);
                    }
                }
            }
        }
    }


    /// <summary>
    ///     Various string extension methods
    /// </summary>
    internal static class StringExtensions
    {
        private static string ToCamelOrPascalCase(string str, Func<char, char> firstLetterTransform) {
            var text = Regex.Replace(str, "([_\\-])(?<char>[a-z])",
                match => match.Groups["char"].Value.ToUpperInvariant(), RegexOptions.IgnoreCase);
            return firstLetterTransform(text[0]) + text.Substring(1);
        }


        /// <summary>
        ///     Convert the string with underscores (this_is_a_test) or hyphens (this-is-a-test) to
        ///     camel case (thisIsATest). Camel case is the same as Pascal case, except the first letter
        ///     is lowercase.
        /// </summary>
        /// <param name="str">String to convert</param>
        /// <returns>Converted string</returns>
        public static string ToCamelCase(this string str) {
            return ToCamelOrPascalCase(str, char.ToLowerInvariant);
        }

        /// <summary>
        ///     Convert the string with underscores (this_is_a_test) or hyphens (this-is-a-test) to
        ///     pascal case (ThisIsATest). Pascal case is the same as camel case, except the first letter
        ///     is uppercase.
        /// </summary>
        /// <param name="str">String to convert</param>
        /// <returns>Converted string</returns>
        public static string ToPascalCase(this string str) {
            return ToCamelOrPascalCase(str, char.ToUpperInvariant);
        }

        /// <summary>
        ///     Convert the string from camelcase (thisIsATest) to a hyphenated (this-is-a-test) or
        ///     underscored (this_is_a_test) string
        /// </summary>
        /// <param name="str">String to convert</param>
        /// <param name="separator">Separator to use between segments</param>
        /// <returns>Converted string</returns>
        public static string FromCamelCase(this string str, string separator) {
            // Ensure first letter is always lowercase
            str = char.ToLower(str[0]) + str.Substring(1);

            str = Regex.Replace(str.ToCamelCase(), "(?<char>[A-Z])",
                match => separator + match.Groups["char"].Value.ToLowerInvariant());
            return str;
        }
    }
}