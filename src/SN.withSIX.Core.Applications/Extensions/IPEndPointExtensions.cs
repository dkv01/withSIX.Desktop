using System.Net;

namespace SN.withSIX.Core.Applications.Extensions
{
    public static class IPEndPointExtensions
    {
        public static string ToHttp(this IPEndPoint ep) => ToProto(ep, "http");

        public static string ToHttps(this IPEndPoint ep) => ToProto(ep, "https");
        private static string ToProto(IPEndPoint ep, string scheme) => scheme + "://" + ep;
    }
}