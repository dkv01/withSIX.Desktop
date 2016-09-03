// <copyright company="SIX Networks GmbH" file="Transfer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SN.withSIX.Core.Logging;
using withSIX.Api.Models.Extensions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SN.withSIX.Core
{
    public static partial class Tools
    {
        public static TransferTools Transfer = new TransferTools();

        #region Nested type: Transfer

        public class TransferTools : IEnableLogging
        {
            static readonly char[] qsSplit = {'?', '&'};
            static readonly char[] splitQsParam = {'='};

            public virtual Dictionary<string, string> GetDictionaryFromQueryString(string qs) {
                Contract.Requires<ArgumentNullException>(qs != null);

                var parts = qs.Split(qsSplit);
                var properties = parts.Skip(1);
                return properties.Select(p => p.Split(splitQsParam, 2))
                    .ToDictionary(ps => ps[0], ps => Uri.UnescapeDataString(ps[1]));
            }

            public string EncodePathIfRequired(Uri uri, string path) => UriPathEncoder.EncodePath(uri, path);

            // TODO: Missing serializersettings?
            public Task<T> GetJson<T>(Uri uri, string token = null) => uri.GetJson<T>(token);

            // TODO: Missing serializersettings?
            public Task<HttpResponseMessage> PostJson(object model, Uri uri, string token = null)
                => DownloaderExtensions.PostJson(model, uri, token);

            public Task<T> GetYaml<T>(Uri uri, string token = null) => DownloaderExtensions.GetYaml<T>(uri, token);

            public Task<HttpResponseMessage> PostYaml(object model, Uri uri, string token = null)
                => DownloaderExtensions.PostYaml(model, uri, token);

            public Uri JoinUri(Uri host, params object[] remotePaths) {
                Contract.Requires<ArgumentNullException>(host != null);
                Contract.Requires<ArgumentNullException>(remotePaths != null);
                Contract.Requires<ArgumentNullException>(remotePaths.Any());

                var remotePath = JoinPaths(remotePaths);
                if (!host.ToString().EndsWith("/"))
                    host = new Uri(host + "/");
                if (remotePath.StartsWith("/"))
                    remotePath = remotePath.Substring(1);
                return new Uri(host, remotePath);
            }

            public string JoinPaths(params object[] parts)
                => string.Join("/", parts.Select(x => x?.ToString().TrimStart('/').TrimEnd('/')));

            class UriPathEncoder
            {
                static readonly string[] encodedSchemes = {"http", "https", "ftp"};
                static readonly string[] altEncodedSchemes = {"zsync", "zsyncs"};

                public static string EncodePath(Uri uri, string path) {
                    if (path.Contains(@"\"))
                        throw new NotSupportedException(@"Path contains \");
                    return EncodingRequired(uri) ? UrlEncodeRemoteFilePath(uri, path) : path;
                }

                static string UrlEncodeRemoteFilePath(Uri uri, string path) => EncodeStandard(path);

                private static string EncodeStandard(string path) => !path.Contains("/")
                    ? Encode(path)
                    : string.Join("/", path.Split('/').Select(Encode));

                static string Encode(string path) => Uri.EscapeUriString(path).Replace("#", "%23");

                static bool EncodingRequired(Uri uri)
                    => encodedSchemes.Contains(uri.Scheme) || altEncodedSchemes.Contains(uri.Scheme);
            }
        }

        #endregion
    }

    public static class DownloaderExtensions
    {
        static readonly DataAnnotationsValidator.DataAnnotationsValidator _validator =
            new DataAnnotationsValidator.DataAnnotationsValidator();

        public static async Task<T> GetJson<T>(this Uri uri, string token = null) {
            var r = await uri.GetJsonText(token).ConfigureAwait(false);
            return r.FromJson<T>();
        }

        public static async Task<string> GetJsonText(this Uri uri, string token = null) {
            using (var client = GetHttpClient()) {
                client.Setup(uri, token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                return await client.GetStringAsync(uri).ConfigureAwait(false);
            }
        }

        // TODO: Missing serializersettings?
        public static async Task<HttpResponseMessage> PostJson(object model, Uri uri, string token = null) {
            _validator.ValidateObject(model);
            using (var client = new HttpClient()) {
                client.Setup(uri, token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using (
                    var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8,
                        "application/json"))
                    return await client.PostAsync(uri, content).ConfigureAwait(false);
            }
        }

        public static async Task<T> GetYaml<T>(Uri uri, string token = null) {
            using (var client = GetHttpClient()) {
                client.Setup(uri, token);
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/yaml"));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
                    "text/html,application/xhtml+xml,application/xml,text/yaml,text/x-yaml,application/yaml,application/x-yaml");

                var r = await client.GetStringAsync(uri).ConfigureAwait(false);
                return
                    new Deserializer(ignoreUnmatched: true).Deserialize<T>(
                        new StringReader(r));
            }
        }

        public static async Task<HttpResponseMessage> PostYaml(object model, Uri uri, string token = null) {
            _validator.ValidateObject(model);
            using (var client = new HttpClient()) {
                client.Setup(uri, token);
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/yaml"));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
                    "text/html,application/xhtml+xml,application/xml,text/yaml,text/x-yaml,application/yaml,application/x-yaml");
                using (
                    var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8,
                        "text/yaml"))
                    return await client.PostAsync(uri, content).ConfigureAwait(false);
            }
        }

        static void Setup(this HttpClient client, Uri uri, string token) {
            HandleUserInfo(client, uri.UserInfo);
            if (token != null && CommonUrls.IsWithSixUrl(uri))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        static void HandleUserInfo(HttpClient client, string userInfo) {
            if (string.IsNullOrWhiteSpace(userInfo))
                return;
            var byteArray = Encoding.ASCII.GetBytes(userInfo);
            var authorizationString = Convert.ToBase64String(byteArray);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authorizationString);
        }

        static HttpClient GetHttpClient() {
            var client = new HttpClient(
                new HttpClientHandler {
                    AutomaticDecompression = DecompressionMethods.GZip
                                             | DecompressionMethods.Deflate
                });
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            return client;
        }

        public sealed class CustomCamelCaseNamingConvention : INamingConvention
        {
            readonly CamelCaseNamingConvention convention = new CamelCaseNamingConvention();

            public string Apply(string value) {
                var s = value == null || !value.StartsWith(":") ? value : value.Substring(1);
                return convention.Apply(s);
            }
        }
    }
}