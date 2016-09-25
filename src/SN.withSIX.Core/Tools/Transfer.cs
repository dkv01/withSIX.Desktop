// <copyright company="SIX Networks GmbH" file="Transfer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SN.withSIX.Core.Logging;
using withSIX.Api.Models;
using withSIX.Api.Models.Extensions;

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
            public Task<string> PostJson(object model, Uri uri, string token = null)
                => model.PostJson(uri, token);

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

    public static class W6DownloaderExtensions
    {
        public static Task<T> GetJson<T>(this Uri uri, string token = null)
            => uri.GetJson<T>(client => Setup(client, uri, token));

        public static Task<string> GetJsonText(this Uri uri, string token = null)
            => uri.GetJsonText(client => Setup(client, uri, token));

        public static Task<string> PostJson(this object model, Uri uri, string token = null)
            => model.PostJson(uri, client => Setup(client, uri, token));

        public static async Task<T> PostJson<T>(this object model, Uri uri, string token = null) {
            var r = await model.PostJson(uri, token).ConfigureAwait(false);
            return r.FromJson<T>();
        }

        public static void Setup(HttpClient client, Uri uri, string token) {
            DownloaderExtensions.HandleUserInfo(client, uri.UserInfo);
            AddTokenIfWithsix(client, uri, token);
        }

        static void AddTokenIfWithsix(HttpClient client, Uri uri, string token) {
            if ((token != null) && CommonUrls.IsWithSixUrl(uri))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}