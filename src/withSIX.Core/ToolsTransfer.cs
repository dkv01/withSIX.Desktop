// <copyright company="SIX Networks GmbH" file="Transfer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;

namespace withSIX.Core
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
            [Obsolete("Use extensions")]
            public Task<string> PostJson(object model, Uri uri, CancellationToken ct = default(CancellationToken),
                    string token = null)
                => model.PostJson(uri, ct, token);

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
}