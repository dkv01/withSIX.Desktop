using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Polly;
using Polly.Retry;
using Refit;
using withSIX.Api.Models.Collections;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Logging;
using withSIX.Mini.Core;
using withSIX.Mini.Core.Social;

namespace withSIX.Mini.Infra.Data.Services
{
    // TODO: Should have a different retry policy for Post! (only on Error 500, or similar?)
    // Also on Retry, we might perhaps swallow a AlreadyExists error code, becacuse it might mean the request did work previously anyway?
    // TODO: Why the Wrap comes with it's own CT call?!
    // TODO: Cant we Wrap automatically and skip a manual class? (the whole idea of Refit)

    [Headers("Accept: application/json")]
    public interface JsonApi { }
    // TODO: Split per API ep, like controllers?
    public interface IW6MainApi : JsonApi
    {
        [Post("/api/stats")]
        Task StatusOverview([Body] InstallStatusOverview overview, CancellationToken ct);

        [Headers("Authorization: Bearer")]
        [Get("/api/groups/{id}/access")]
        Task<GroupAccess> GroupAccess(Guid id, CancellationToken ct);

        [Headers("Authorization: Bearer")]
        [Get("/api/groups/{id}/contents")]
        Task<List<GroupContent>> GroupContent(Guid id, CancellationToken ct);

        // Auth is optional
        [Headers("Authorization: Bearer")]
        [Get("/api/collections")]
        Task<List<CollectionModelWithLatestVersion>> Collections(Guid gameId, string ids2, CancellationToken ct);
    }

    public interface IW6CDNApi : JsonApi
    {
        [Get("/hashes-{gameId}.json.gz")]
        Task<withSIX.Mini.Core.Games.ApiHashes> Hashes(Guid gameId, CancellationToken ct);

        [Get("/mods-{gameId}.json.gz")]
        Task<List<ModClientApiJson>> Mods(Guid gameId, string version, CancellationToken ct);
    }


    public class SteamServiceSessionHttp { }

    public class W6Api : IW6Api
    {
        private readonly IW6MainApi _api;
        private readonly IW6CDNApi _cdn;
        private readonly RetryPolicy _policy;
        public const string ApiCdn = "http://withsix-api.azureedge.net/api/v3";

        public static IW6MainApi Create([NotNull] Func<Task<string>> authGetter)
            => Create<IW6MainApi>(CommonUrls.SocialApiUrl, authGetter);

        public static IW6CDNApi Create() => Create<IW6CDNApi>(new Uri(ApiCdn));

        public W6Api(IW6MainApi api, IW6CDNApi cdn, RetryPolicy policy) {
            _api = api;
            _cdn = cdn;
            _policy = policy;
        }

        public Task<List<CollectionModelWithLatestVersion>> Collections(Guid gameId, List<Guid> ids,
                CancellationToken ct) =>
            Wrap(t => _api.Collections(gameId, string.Join(",", ids), t), ct);

        public Task<Core.Games.ApiHashes> Hashes(Guid gameId, CancellationToken ct) =>
            Wrap(t => _cdn.Hashes(gameId, t), ct);

        public Task<List<ModClientApiJson>> Mods(Guid gameId, string version, CancellationToken ct) =>
            Wrap(t => _cdn.Mods(gameId, version, t), ct);

        public Task<List<GroupContent>> GroupContent(Guid id, CancellationToken ct)
            => Wrap(t => _api.GroupContent(id, t), ct);

        // TODO: Might this also be a candidate for Oauth2/Connect?
        public Task<GroupAccess> GroupAccess(Guid id, CancellationToken ct)
            => Wrap(t => _api.GroupAccess(id, t), ct);

        public Task CreateStatusOverview(InstallStatusOverview stats, CancellationToken ct)
            => Wrap(t => _api.StatusOverview(stats, t), ct);

        private Task<T> Wrap<T>(Func<CancellationToken, Task<T>> fnc, CancellationToken ct)
            => _policy.ExecuteAsync(fnc, ct);

        private Task Wrap(Func<CancellationToken, Task> fnc, CancellationToken ct)
            => _policy.ExecuteAsync(fnc, ct);

        // todo: the lifetime of the httpclient should generally be singleton
        // however care needs to be taken because of DNS cache etc, which might make cloud hosts hopping dns not so fast informing our client :)
        public static T Create<T>(Uri baseAddr, Func<Task<string>> authGetter = null)
            => RestService.For<T>(CreateHttpClient(baseAddr, authGetter), new RefitSettings {
                AuthorizationHeaderValueGetter = authGetter,
                JsonSerializerSettings = JsonSupport.DefaultSettings
            });

        class AuthenticatedHttpClientHandler : DelegatingHandler
        {
            readonly Func<Task<string>> getToken;

            public AuthenticatedHttpClientHandler(Func<Task<string>> getToken, HttpMessageHandler innerHandler = null)
                : base(innerHandler ?? new HttpClientHandler()) {
                if (getToken == null) throw new ArgumentNullException("getToken");
                this.getToken = getToken;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
                // See if the request has an authorize header
                var auth = request.Headers.Authorization;
                if (auth != null) {
                    var token = await getToken().ConfigureAwait(false);
                    request.Headers.Authorization = new AuthenticationHeaderValue(auth.Scheme, token);
                }

                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        private static HttpClient CreateHttpClient(Uri baseAddr, Func<Task<string>> authGetter) {
            HttpMessageHandler inner = new HttpClientHandler {
                AutomaticDecompression = DecompressionMethods.GZip
                                         | DecompressionMethods.Deflate
            };
            if (authGetter != null)
                inner = new AuthenticatedHttpClientHandler(authGetter, inner);
            var httpClient = new HttpClient(inner);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            httpClient.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.BaseAddress = baseAddr;
            return httpClient;
        }

        // TODO: PollyAppraoch
        public static RetryPolicy CreatePolicy() => Policy
            .Handle<ApiException>(TransientExceptionHelper.IsTransient)
            .Or<WebException>()
            .WaitAndRetryAsync(new[] {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
            }, (exception, span, arg3, arg4) => {
                MainLog.Logger.Warn($"Error catched during api call, retrying {arg3}: {exception.Message}");
                MainLog.Logger.FormattedDebugException(exception, "Error catched during api call");
            });

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
        string name;
        IEnumerable<T> values;

        public static CsvList2<T> Create(List<T> values, string name) {
            return new CsvList2<T> { values = values, name = name };
        }

        public override string ToString() {
            if (values == null)
                return null;
            return string.Join($"&ids=", values);
        }
    }
}