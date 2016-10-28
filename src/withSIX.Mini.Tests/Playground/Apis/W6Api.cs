using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Refit;
using withSIX.Api.Models.Collections;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Mini.Core.Social;
using withSIX.Mini.Infra.Data;

namespace withSIX.Mini.Tests.Playground.Apis
{
    // TODO: Split per API ep, like controllers?
    public interface IW6Api
    {
        [Post("/api/stats")]
        Task StatusOverview([Body] InstallStatusOverview overview);

        [Headers("Authorization: Bearer")]
        [Get("/api/groups/{id}/access")]
        Task<GroupAccess> GroupAccess(Guid id);

        [Headers("Authorization: Bearer")]
        [Get("/api/groups/{id}/contents")]
        Task<List<GroupContent>> GroupContent(Guid id);

        // Auth is optional
        [Headers("Authorization: Bearer")]
        [Get("/api/collections")]
        Task<List<CollectionModelWithLatestVersion>> Collections(Guid gameId, CsvList<Guid> ids);

        [Get(W6Api.ApiCdn + "/api/v3/hashes-{gameId}.json.gz")]
        Task<ApiHashes> Hashes(Guid gameId);

        [Get(W6Api.ApiCdn + "/api/v3/mods-{gameId}.json.gz")]
        Task<ApiHashes> Mods(Guid gameId);
    }

    // https://github.com/paulcbetts/refit/issues/93
    // doesnt include the multiple &key=, so should adjust server then, if possible
    public class CsvList<T>
    {
        IEnumerable<T> values;

        // Unfortunately, you have to use a concrete type rather than IEnumerable<T> here
        public static implicit operator CsvList<T>(List<T> values) {
            return new CsvList<T> { values = values };
        }

        public override string ToString() {
            if (values == null)
                return null;
            return string.Join(",", values);
        }
    }

    // bad
    public class CsvList2<T>
    {
        IEnumerable<T> values;

        string name;

        public static CsvList2<T> Create(List<T> values, string name) {
            return new CsvList2<T> { values = values, name = name };
        }

        public override string ToString() {
            if (values == null)
                return null;
            return string.Join($"&ids=", values);
        }
    }

    public static class W6Api
    {
        public const string ApiCdn = "http://withsix-api.azureedge.net";
        public static IW6Api Create(Func<Task<string>> authGetter) {
            // todo: the lifetime of the httpclient should generally be singleton
            // however care needs to be taken because of DNS cache etc, which might make cloud hosts hopping dns not so fast informing our client :)
            var httpClient = DownloaderExtensions.GetHttpClient();
            httpClient.BaseAddress = CommonUrls.SocialApiUrl;
            return RestService.For<IW6Api>(httpClient, new RefitSettings {
                AuthorizationHeaderValueGetter = authGetter,
                JsonSerializerSettings = JsonSupport.DefaultSettings
            });
        }

        // TODO: PollyAppraoch
        public static PolicyBuilder CreatePolicy() {
            return Policy.Handle<ApiException>().Or<WebException>();
        }

        // Manual approach
        public class TransientExceptionHelper
        {
            private static readonly ISet<HttpStatusCode> TransientErrorStatusCodes = new HashSet<HttpStatusCode>(new[] {
                HttpStatusCode.BadGateway,
                HttpStatusCode.GatewayTimeout,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.RequestTimeout
            });
            public static bool IsTransient(Exception exception) {
                var apiException = exception as ApiException;
                if (apiException != null) {
                    return TransientErrorStatusCodes.Contains(apiException.StatusCode);
                }
                return exception is HttpRequestException || exception is OperationCanceledException;
            }
        }
        /*
// Usage
catch (Exception transientEx) when(TransientExceptionHelper.IsTransient(transientEx))
{
    // Handle retries
}
         */
    }

    public class SteamServiceSessionHttp {}
}
