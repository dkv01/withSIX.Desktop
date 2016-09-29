// <copyright company="SIX Networks GmbH" file="GeoIpService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.IO;
using System.Net;
using withSIX.Core.Services.Infrastructure;

namespace withSIX.Core.Infra.Services
{
    public class GeoIpService : IGeoIpService, IInfrastructureService
    {
        readonly LookupService _lookupService;
        readonly ResourceService _resources;

        public GeoIpService() {
            _resources = new ResourceService();
            _lookupService = new LookupService(GeoIpDb(), LookupService.GEOIP_MEMORY_CACHE);
        }

        public string GetCountryCode(IPAddress ip) {
            var country = _lookupService.getCountry(ip);
            return country?.getCode();
        }

        Stream GeoIpDb() => _resources.GetResource("config.GeoIP.dat");
    }
}