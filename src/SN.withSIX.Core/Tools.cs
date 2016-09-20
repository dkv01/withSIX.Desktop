// <copyright company="SIX Networks GmbH" file="Tools.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using SN.withSIX.Core.Services;
using SN.withSIX.Core.Services.Infrastructure;

namespace SN.withSIX.Core
{
    public class ToolsServices : IDomainService
    {
        public ToolsServices(IProcessManager pm, Lazy<IWCFClient> wcfClient, Lazy<IGeoIpService> geoService, ICompressionUtil compression, IUacHelper uacHelper) {
            GeoService = geoService;
            Compression = compression;
            UacHelper = uacHelper;
            ProcessManager = pm;
            WCFClient = wcfClient;
        }

        public Lazy<IGeoIpService> GeoService { get; }
        public ICompressionUtil Compression { get; }
        public IUacHelper UacHelper { get; }
        public IProcessManager ProcessManager { get; }
        public Lazy<IWCFClient> WCFClient { get; }
    }

    public static partial class Tools
    {
        static Lazy<IWCFClient> _wcfClient;
        public static readonly string DefaultSizeReturn = string.Empty;
        public static IProcessManager ProcessManager { get; private set; }
        public static Lazy<IGeoIpService> Geo { get; private set; }

        public static IUacHelper UacHelper { get; private set; }

        public static Func<string, string, Exception, Task> InformUserError { get; set; }

        private static ICompressionUtil _compressionUtil { get; set; }
        public static ICompressionUtil CompressionUtil => _compressionUtil;

        public static void RegisterServices(ToolsServices services) {
            ProcessManager = services.ProcessManager;
            _wcfClient = services.WCFClient;
            _compressionUtil = services.Compression;
            Geo = services.GeoService;
            UacHelper = services.UacHelper;
        }
    }
}