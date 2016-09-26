using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Core.Extensions
{
    public static class W6DownloaderExtensions
    {
        public static Task<T> GetJson<T>(this Uri uri, CancellationToken ct = default(CancellationToken), string token = null)
            => uri.GetJson<T>(ct, client => Setup(client, uri, token));

        public static Task<string> GetJsonText(this Uri uri, CancellationToken ct = default(CancellationToken), string token = null)
            => uri.GetJsonText(ct, client => Setup(client, uri, token));

        public static Task<string> PostJson(this object model, Uri uri, CancellationToken ct = default(CancellationToken), string token = null)
            => model.PostJson(uri, ct, client => Setup(client, uri, token));

        public static async Task<T> PostJson<T>(this object model, Uri uri, CancellationToken ct = default(CancellationToken), string token = null) {
            var r = await model.PostJson(uri, ct, token).ConfigureAwait(false);
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