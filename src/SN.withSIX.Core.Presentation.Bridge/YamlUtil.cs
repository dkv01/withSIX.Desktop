using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using Newtonsoft.Json;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Sync.Core;
using SN.withSIX.Sync.Core.Legacy;
using withSIX.Api.Models;
using withSIX.Api.Models.Extensions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SN.withSIX.Core.Presentation
{
    // TODO: YamlDotNet is currently not PCL compatible
    // So we want to move this out to the highest layer, and DI it.
    public class YamlUtil : IYamlUtil, IPresentationService
    {
        [Obsolete("Use extensions")]
        public Task<T> GetYaml<T>(Uri uri, CancellationToken ct = default(CancellationToken), string token = null) => uri.GetYaml<T>(ct, token);

        public void PrintMapping(YamlMappingNode mapping) {
            Contract.Requires<ArgumentNullException>(mapping != null);

            foreach (var entry in mapping.Children) {
                var key = ((YamlScalarNode)entry.Key).Value;
                var value = string.Empty;

                try {
                    value = ((YamlScalarNode)entry.Value).Value;
                } catch (Exception) { }

                Console.WriteLine("{0}: {1}", key, value);
            }
        }

        public T NewFromYamlFile<T>(IAbsoluteFilePath fileName) => NewFromYaml<T>(fileName.ReadAllText());

        public T NewFromYaml<T>(string yaml) {
            try { 
                return yaml.FromYaml<T>();
            } catch (Exception ex) {
                throw new YamlParseException(ex.Message, ex);
            }
        }

        public Dictionary<string, string> ToStringDictionary(IDictionary<YamlNode, YamlNode> list) {
            Contract.Requires<ArgumentNullException>(list != null);

            return list.ToDictionary(ent => ((YamlScalarNode)ent.Key).Value,
                ent => ((YamlScalarNode)ent.Value).Value);
        }

        public Dictionary<string, int> ToIntDictionary(IDictionary<YamlNode, YamlNode> list) {
            Contract.Requires<ArgumentNullException>(list != null);

            return list.ToDictionary(ent => ((YamlScalarNode)ent.Key).Value,
                ent => ((YamlScalarNode)ent.Value).Value.TryInt());
        }

        public Dictionary<string, long> ToLongDictionary(IDictionary<YamlNode, YamlNode> list) {
            Contract.Requires<ArgumentNullException>(list != null);

            return list.ToDictionary(ent => ((YamlScalarNode)ent.Key).Value,
                ent => ((YamlScalarNode)ent.Value).Value.TryLong());
        }

        public Dictionary<string, string> GetStringDictionaryInternal(YamlNode node) {
            Contract.Requires<ArgumentNullException>(node != null);

            var mapping = node as YamlMappingNode;
            if (mapping == null) {
                var mapping2 = node as YamlScalarNode;
                if (mapping2 != null && string.IsNullOrEmpty(mapping2.Value))
                    return null;

                var mapping3 = node as YamlSequenceNode;
                if (mapping3 != null) {
                    return mapping3
                        .Select(n => ((YamlSequenceNode)n).ToArray())
                        .ToDictionary(ar => ((YamlScalarNode)ar[0]).Value, ar => ((YamlScalarNode)ar[1]).Value);
                }

                throw new YamlExpectedOtherNodeTypeException("Expected YamlMappingNode");
            }
            return ToStringDictionary(mapping.Children);
        }

        public string[] GetStringArrayInternal(YamlNode node) {
            Contract.Requires<ArgumentNullException>(node != null);

            var mapping = node as YamlSequenceNode;
            if (mapping == null) {
                var mapping2 = node as YamlScalarNode;
                if (mapping2 != null && string.IsNullOrEmpty(mapping2.Value))
                    return null;
                throw new YamlExpectedOtherNodeTypeException("Expected YamlSequenceNode");
            }
            return mapping.Children.Select(x => ((YamlScalarNode)x).Value).ToArray();
        }

        YamlNode GetRootNode(YamlStream yaml) {
            Contract.Requires<ArgumentNullException>(yaml != null);

            if (yaml.Documents.Count == 0)
                throw new YamlParseException("Contains no documents");
            return yaml.Documents[0].RootNode;
        }

        public YamlMappingNode GetMapping(YamlStream yaml) {
            Contract.Requires<ArgumentNullException>(yaml != null);

            var node = GetRootNode(yaml);
            var mapped = node as YamlMappingNode;
            if (mapped == null)
                throw new YamlParseException("Not a mapping");
            return mapped;
        }

        public DateTime GetDateTimeOrDefault(YamlNode node) {
            Contract.Requires<ArgumentNullException>(node != null);

            var val = ((YamlScalarNode)node).Value;
            DateTime dt;
            DateTime.TryParse(val, out dt);
            return dt;
        }

        public string GetStringOrDefault(YamlNode node) {
            Contract.Requires<ArgumentNullException>(node != null);

            return ((YamlScalarNode)node).Value;
        }

        public long GetLongOrDefault(YamlNode node) {
            Contract.Requires<ArgumentNullException>(node != null);

            return ((YamlScalarNode)node).Value.TryLong();
        }

        public int GetIntOrDefault(YamlNode node) {
            Contract.Requires<ArgumentNullException>(node != null);

            return ((YamlScalarNode)node).Value.TryInt();
        }

        public bool GetBoolOrDefault(YamlNode node) {
            Contract.Requires<ArgumentNullException>(node != null);

            return ((YamlScalarNode)node).Value.TryBool();
        }

        public string[] GetStringArray(YamlNode node) => GetStringArrayInternal(node) ?? new string[0];

        public Dictionary<string, string> GetStringDictionary(YamlNode node)
            => GetStringDictionaryInternal(node) ?? new Dictionary<string, string>();

        public string ToYaml(object graph) {
            Contract.Requires<ArgumentNullException>(graph != null);

            var serializer = new Serializer();
            using (var text = new StringWriter()) {
                text.Write("--- \r\n");
                var emitter = new Emitter(text);
                serializer.Serialize(emitter, graph);
                return text.ToString();
            }
        }

        public void ToYamlFile(IBaseYaml graph, IAbsoluteFilePath fileName)
            => YamlIoExtensions._SaveYaml(graph.ToYaml(), fileName); // We call the graph ToYaml() because it manually serialises..
    }

    public static class YamlIoExtensions
    {
        internal static void _SaveYaml(string yaml, IAbsoluteFilePath fileName) {
            Contract.Requires<ArgumentNullException>(yaml != null);
            Contract.Requires<ArgumentNullException>(fileName != null);

            Tools.FileTools.SafeIO.SafeSave(x => {
                using (var sr = new StreamWriter(x.ToString()))
                    sr.Write(yaml);
            }, fileName);
        }

        internal static YamlStream ReadYaml(this string yaml) {
            Contract.Requires<ArgumentNullException>(yaml != null);

            using (var input = new StringReader(yaml)) {
                var yaml1 = new YamlStream();
                yaml1.Load(input);
                return yaml1;
            }
        }

        internal static YamlStream ReadYamlFile(this IAbsoluteFilePath yaml) {
            Contract.Requires<ArgumentNullException>(yaml != null);

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
        public static Task<T> GetYaml<T>(this Uri uri, CancellationToken ct = default(CancellationToken), string token = null)
            => uri.GetYaml<T>(ct, client => W6DownloaderExtensions.Setup(client, uri, token));

        public static Task<string> GetYamlText(this Uri uri, CancellationToken ct = default(CancellationToken), string token = null)
            => uri.GetYamlText(ct, client => W6DownloaderExtensions.Setup(client, uri, token));

        public static Task<string> PostYaml(this object model, Uri uri, CancellationToken ct = default(CancellationToken), string token = null)
            => model.PostJson(uri, ct, client => W6DownloaderExtensions.Setup(client, uri, token));
    }

    public static class DownloaderYamlExtensions
    {

        private const string YamlMimeType = "text/yaml";
        private const string YamlMimeAcceptType =
            "text/html,application/xhtml+xml,application/xml,text/yaml,text/x-yaml,application/yaml,application/x-yaml";

        public static async Task<T> GetYaml<T>(this Uri uri, CancellationToken ct = default(CancellationToken), Action<HttpClient> setup = null) {
            var c = await uri.GetYamlText(ct, setup).ConfigureAwait(false);
            return c.FromYaml<T>();
        }

        public static T FromYaml<T>(this string c) {
            using (var stringReader = new StringReader(c))
                return
                    new Deserializer(ignoreUnmatched: true, namingConvention: new CustomCamelCaseNamingConvention())
                        .Deserialize<T>(stringReader);
        }

        public static async Task<string> GetYamlText(this Uri uri, CancellationToken ct = default(CancellationToken), Action<HttpClient> setup = null) {
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

        public static async Task<string> PostYaml(object model, Uri uri, CancellationToken ct = default(CancellationToken), Action<HttpClient> setup = null) {
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

        public sealed class CustomCamelCaseNamingConvention : INamingConvention
        {
            readonly INamingConvention convention = new UnderscoredNamingConvention();

            public string Apply(string value) {
                var s = (value == null) || !value.StartsWith(":") ? value : value.Substring(1);
                return convention.Apply(s);
            }
        }
    }
}
