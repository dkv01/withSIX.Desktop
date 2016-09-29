// <copyright company="SIX Networks GmbH" file="ApiGetBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace withSIX.Steam.Api.SteamKit.WebApi
{
    public abstract class ApiBase<TReturn, TRequest>
        where TReturn : class
        where TRequest : class, IApiRequest
    {
        protected static string CalculateMD5Hash(string input) {
            // step 1, calculate MD5 hash from input
            var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            var sb = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("X2"));
            return sb.ToString();
        }
    }

    public abstract class ApiGetBase<TReturn, TRequest> : ApiBase<TReturn, TRequest>
        where TReturn : class
        where TRequest : class, IApiRequest
    {
        public static async Task<TReturn> Get(TRequest request, bool loadFromFile = false) {
            var uri = request.ToUri();
            var fn = CalculateMD5Hash(uri.ToString()) + ".cache";
            if (loadFromFile && File.Exists(fn))
                return JsonConvert.DeserializeObject<TReturn>(File.ReadAllText(fn));
            var value = await GetWeb(uri).ConfigureAwait(false);
            if (loadFromFile)
                File.WriteAllText(fn, value);
            return JsonConvert.DeserializeObject<TReturn>(value);
        }

        static async Task<string> GetWeb(Uri uri) {
            using (var client = new HttpClient())
                return await client.GetStringAsync(uri);
        }
    }

    public abstract class ApiPostBase<TReturn, TRequest> : ApiBase<TReturn, TRequest>
        where TReturn : class
        where TRequest : class, IApiRequest
    {
        public static async Task<TReturn> Get(TRequest request, bool loadFromFile = false) {
            var uri = request.ToUri();
            var fn = CalculateMD5Hash(uri.ToString()) + ".cache";
            if (loadFromFile && File.Exists(fn))
                return JsonConvert.DeserializeObject<TReturn>(File.ReadAllText(fn));
            var value = await GetWeb(uri).ConfigureAwait(false);
            if (loadFromFile)
                File.WriteAllText(fn, value);
            return JsonConvert.DeserializeObject<TReturn>(value);
        }

        static async Task<string> GetWeb(Uri uri) {
            using (var client = new HttpClient()) {
                var request = new HttpRequestMessage {
                    Method = HttpMethod.Post
                };
                request.Content = new StringContent(uri.Query.TrimStart('?'));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                var requestUri = uri.AbsoluteUri.Substring(0, uri.AbsoluteUri.IndexOf("/?") + 1);
                return
                    await
                        client.PostAsync(requestUri, request.Content)
                            .Result.Content.ReadAsStringAsync()
                            .ConfigureAwait(false);
            }
        }
    }
}