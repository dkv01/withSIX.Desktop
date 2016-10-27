// <copyright company="SIX Networks GmbH" file="CommonUrls.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace withSIX.Core
{
    public static class Bla
    {
        public static CommonUrls.HostType GetHostType() {
            if (Common.Flags.Staging) {
#if DEBUG
                return CommonUrls.HostType.Local2;
#else
                return CommonUrls.HostType.Staging;
#endif
            }

            return CommonUrls.HostType.Production;
        }

        /// <summary>
        ///     Together with the AcceptAllCertifications method right
        ///     below this causes to bypass errors caused by SLL-Errors.
        /// </summary>
        public static void IgnoreBadCertificates() {
            //ServicePointManager.ServerCertificateValidationCallback = AcceptAllCertifications;
        }

        /// <summary>
        ///     In Short: the Method solves the Problem of broken Certificates.
        ///     Sometime when requesting Data and the sending Webserverconnection
        ///     is based on a SSL Connection, an Error is caused by Servers whoes
        ///     Certificate(s) have Errors. Like when the Cert is out of date
        ///     and much more... So at this point when calling the method,
        ///     this behaviour is prevented
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certification"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns>true</returns>
        static bool AcceptAllCertifications(object sender, X509Certificate certification, X509Chain chain,
            SslPolicyErrors sslPolicyErrors) => true;
    }

    public static class CommonUrls
    {
        public enum HostType
        {
            Production,
            Staging,
            Local2,
            Local
        }

        public static Uri FlashUri { get; } =
            new Uri(
                "https://fpdownload.macromedia.com/get/flashplayer/current/licensing/win/install_flash_player_21_ppapi.exe")
            ;
        public static Uri UsercontentCdnProduction { get; } =
            new Uri("https://" + Buckets.WthSiUsercontentProduction.CdnHostname);
        public static string TwitterUrl { get; } = @"https://twitter.com/SixNetworks";
        public static string FacebookUrl { get; } = @"https://www.facebook.com/withSIX";
        public static string GplusUrl { get; } = @"https://plus.google.com/104785560298357015306";
        public static Uri ApiCdnUrl { get; } = new Uri("http://" + Buckets.WithSixApi.CdnHostname);
        public static Uri ImageCdn { get; } = new Uri("https://withsix-img.azureedge.net");
        public static Uri PublishApiUrl { get; } = UrlBuilder.GetUri("https", "publish-api");
        public static string ContentApiVersion { get; } = "2";
        public static Uri ApiUrl { get; } = UrlBuilder.GetUri("https", UrlBuilder.Sites.Auth);
        public static Uri CdnUrl2 { get; } = new Uri("http://cdn2.withsix.com");
        public static string RemoteSoftwarePath { get; } = "software/withSIX/";
        public static Uri MainUrl { get; } = UrlBuilder.GetUri("http", "");
        public static Uri MainSslUrl { get; } = UrlBuilder.GetUri("https", "");
        public static Uri ConnectUrlHttp { get; } = new Uri(MainUrl, "me/");
        public static Uri PlayUrl { get; } = new Uri(MainUrl, "p/");
        public static Uri ConnectUrl { get; } = new Uri(MainSslUrl, "me/");
        public static Uri SocialApiUrl => ApiUrl;
        public static Uri LoginUrl => ConnectUrl;
        public static Uri RegisterUrl { get; } = new Uri(MainUrl, @"register");
        public static Uri AccountSettingsUrl { get; } = new Uri(ConnectUrl, @"settings");
        public static Uri ClientUrl { get; } = new Uri(ApiUrl, @"client");
        public static Uri BlogUrl { get; } = new Uri(MainUrl, "blog");
        public static Uri SupportUrl { get; } = new Uri(MainUrl, "support");
        public static Uri ChangelogUrl { get; } = new Uri(MainUrl, "changelog");
        public static Uri SoftwareUpdateUri { get; } = new Uri(CdnUrl2, RemoteSoftwarePath);
        public static Uri Ws1Url { get; } = UrlBuilder.GetUri("https", "ws1");
        public static Uri SignalrApi => ApiUrl;

        public static bool IsWithSixUrl(string url) => IsWithSixUrl(new Uri(url));

        public static bool IsWithSixUrl(Uri targetURL) {
            var host = targetURL.Host.ToLower();

            return host.EndsWith("withsix.com") || host.EndsWith("withsix.net")
#if DEBUG
                   || host.EndsWith("localhost")
#endif
                ;
        }

        public static class AuthorizationEndpoints
        {
            static readonly Uri baseAddress = UrlBuilder.GetUri("https", "auth");
            public static string SyncClientName { get; } = "mini";
            public static Uri AuthorizeEndpoint { get; } = new Uri(baseAddress, "/identity/connect/authorize");
            public static Uri LogoutEndpoint { get; } = new Uri(baseAddress, "/identity/connect/endsession");
            public static Uri TokenEndpoint { get; } = new Uri(baseAddress, "/identity/connect/token");
            public static Uri UserInfoEndpoint { get; } = new Uri(baseAddress, "/identity/connect/userinfo");
            public static Uri IdentityTokenValidationEndpoint { get; } = new Uri(baseAddress,
                "/identity/connect/identitytokenvalidation");
            public static Uri TokenRevocationEndpoint { get; } = new Uri(baseAddress, "/identity/connect/revocation");
            public static Uri LocalCallback { get; } = new Uri("https://localhost/formsclient");
            public static Uri LocalCallbackMini { get; } = new Uri("oob://localhost/wpfclient");
        }

        public static class UrlBuilder
        {
            public static readonly IDictionary<string, int> SiteHttpPortsLocalhost = new Dictionary<string, int> {
                {Sites.Auth, 80},
                {Sites.Admin, 9000},
                {Sites.Main, 9000},
                {Sites.Connect, 9000},
                {Sites.Play, 9000},
                {Sites.Develop, 9000},
                {"publish-api", 80},
                {"ws1", 80},
                {Sites.Api, 80},
                {Sites.Api2, 80}
            };
            public static readonly IDictionary<string, int> SiteHttpsPortsLocalhost = new Dictionary<string, int> {
                {Sites.Auth, 443},
                {Sites.Admin, 9001},
                {Sites.Main, 9001},
                {Sites.Connect, 9001},
                {Sites.Play, 9001},
                {Sites.Develop, 9001},
                {"publish-api", 443},
                {"ws1", 443},
                {Sites.Api, 443},
                {Sites.Api2, 443}
            };
            static readonly IDictionary<string, ConcurrentDictionary<string, Uri>> cache =
                new Dictionary<string, ConcurrentDictionary<string, Uri>> {
                    {"http", new ConcurrentDictionary<string, Uri>()},
                    {"https", new ConcurrentDictionary<string, Uri>()}
                };

            public static string GetUrl(string scheme, string site = "") => GetUri(scheme, site).ToString().TrimEnd('/');

            public static Uri GetLoginUri() => new Uri(GetUri("https", "connect"), "/login");

            public static string GetLoginUrl() => GetLoginUri().ToString().TrimEnd('/');

            public static Uri GetUri(string scheme, string site = "")
                => cache[scheme.ToLower()].GetOrAdd(site, key => GetUriInternal(scheme, site));

            static Uri GetUriInternal(string scheme, string site) {
                const string localhost = "localhost";
                return BuildUri(Environments.Host == localhost ? localhost : GetHostname(Environments.Host, site),
                    scheme,
                    GetPort(scheme, site));
            }

            static int GetPort(string scheme, string site = "") {
                switch (Environments.Environment) {
                case "staging":
                    return GetStagingPort(scheme, site);
                case "production":
                    return GetProductionPort(scheme, site);
                case "local2":
                    return GetLocalhostPort(scheme, site);
                default: {
                    return GetLocalhostPort(scheme, site);
                }
                }
            }

            static int GetLocalhostPort(string scheme, string site = "") {
                if (site == "www")
                    site = "";
                return scheme == "http"
                    ? SiteHttpPortsLocalhost[site]
                    : SiteHttpsPortsLocalhost[site];
            }

            static int GetStagingPort(string scheme, string site = "") {
                if (site == "www")
                    site = "";
                return scheme == "http"
                    ? 80
                    : 443;
            }

            static int GetProductionPort(string scheme, string site = "") {
                if (site == "www")
                    site = "";
                return scheme == "http"
                    ? 80
                    : 443;
            }

            static string GetHostname(string hostName, string site = "") {
                var sub = site + (site.Length > 0 ? "." : "");
                return sub + hostName;
            }

            static Uri BuildUri(string host, string scheme, int? portNumber = null) => portNumber.HasValue
                ? new UriBuilder(scheme, host, portNumber.Value).Uri
                : new UriBuilder(scheme, host).Uri;

            public static class Sites
            {
                public const string Connect = "connect";
                public const string Main = ""; // "www"
                public const string Play = "play";
                public const string Auth = "auth";
                public const string Api = "api";
                public const string Api2 = "api2";
                public const string Develop = "develop";
                public const string Admin = "admin";
            }
        }
    }

    public static class Buckets
    {
        public static Bucket WithSixApi { get; } = new AmazonBucket("withsix-api", Environments.Environment);
        public static Bucket WithSixUsercontent { get; } = new AmazonBucket("withsix-usercontent",
            Environments.Environment);
        public static Bucket WthSiUsercontentProduction { get; } = new AmazonBucket("withsix-usercontent",
            Environments.Production);
        public static Bucket[] All { get; } = {WithSixApi, WithSixUsercontent};
    }

    public class AmazonBucket : Bucket
    {
        internal AmazonBucket(string value, string environment) : base(value, "s3-eu-west-1.amazonaws.com", environment) {}
    }

    public class Bucket
    {
        internal Bucket(string value, string host, string environment) {
            Name = value;
            switch (environment) {
            case Environments.Local:
                value = value + "-dev";
                break;
            case Environments.Local2:
                value = value + "-dev";
                break;
            case Environments.Staging:
                value = value + "-staging";
                break;
            }

            Value = value;
            Hostname = value + "." + host;
            CdnHostname = environment == Environments.Production
                ? value + ".azureedge.net"
                : Hostname;
        }

        public string Hostname { get; }
        public string CdnHostname { get; }
        public string Name { get; }
        string Value { get; }

        public override string ToString() => this;

        public static implicit operator string(Bucket v) => v.Value;
    }

    public static class Environments
    {
        public const string Production = "production";
        public const string Preview = "preview";
        public const string Staging = "staging";
        public const string Local = "local";
        public const string Local2 = "local2";
        public static readonly string Environment;
        static readonly string[] productionHosts = {
            "withsix.com", "www.withsix.com", "connect.withsix.com", "play.withsix.com", "admin.withsix.com",
            "auth.withsix.com"
        };
        static readonly string[] stagingHosts = {
            "staging.withsix.com", "www.staging.withsix.com", "connect.staging.withsix.com", "play.staging.withsix.com",
            "admin.staging.withsix.com", "auth.staging.withsix.com"
        };
        static readonly string[] previewHosts = {
            "preview.withsix.com", "www.preview.withsix.com", "connect.preview.withsix.com", "play.preview.withsix.com",
            "admin.preview.withsix.com", "auth.preview.withsix.com"
        };
        static readonly string[] localCloudHosts;
        static readonly string[] localHosts = {
            "local.withsix.net", "www.local.withsix.net", "connect.local.withsix.net", "play.local.withsix.net",
            "admin.local.withsix.net", "auth.local.withsix.net"
        };
        static readonly string[] environments = {Production, Staging, Local, Local2};

        static Environments() {
            Environment = Environment = Bla.GetHostType().ToString().ToLower();
            if (!environments.Contains(Environment))
                throw new NotSupportedException("Unsupported environment: " + Environment);
            IsLocal = (Environment == Local) || (Environment == Local2);
            localCloudHosts = GetLocalHosts();
            Host = GetHost();
            Origins = GetOrigins().ToArray();
            CdnUrl = GetCdnUrl();
            //if (Common.Flags.Staging)
            //  Bla.IgnoreBadCertificates();
        }

        public static string[] Origins { get; }
        public static string RootPath { get; }

        public static string Host { get; }
        public static bool IsLocal { get; set; }
        public static string CdnUrl { get; }

        static string GetHost() {
            switch (Environment) {
            case "staging":
                return "staging.withsix.com";
            case "production":
                return "withsix.com";
            case "local2":
                return GetLocalHost();
            default: {
                return "localhost";
            }
            }
        }

        static string GetLocalHost() {
            const string host = "local.withsix.net";
            return host;
        }

        static string[] GetLocalHosts() => localHosts;

        static string JoinMapping(string mapping, string hostname) {
            var split = hostname.Split('.').ToList();
            split.Insert(split.Count == 3 ? 0 : 1, mapping);
            return string.Join(".", split);
        }

        static string GetCdnUrl() {
            switch (Environment) {
            case Local:
                // TODO: LocalHost + ports.. and use http?
                return "//" + GetLocalHost() + "/cdn";
            case Local2:
                return "//" + GetLocalHost() + "local.withsix.net/cdn";
            case Production:
                return "//az667488.vo.msecnd.net";
            case Staging:
                return "//az668256.vo.msecnd.net";
            default: {
                throw new NotSupportedException();
            }
            }
        }

        static IEnumerable<string> GetOrigins() => GetProtocolUrls(productionHosts)
            .Concat(GetProtocolUrls(stagingHosts))
            .Concat(GetProtocolUrls(previewHosts)).Concat(GetLHosts());

        private static IEnumerable<string> GetLHosts() => Enumerable.Repeat("http://localhost:9000", 1)
            .Concat(
                localCloudHosts.Select(x => "http://" + x + ":9000")
                    .Concat(localCloudHosts.Select(x => "https://" + x + ":9001")));

        private static IEnumerable<string> GetProtocolUrls(string[] hosts)
            => hosts.Select(x => "http://" + x).Concat(hosts.Select(x => "https://" + x));
    }
}