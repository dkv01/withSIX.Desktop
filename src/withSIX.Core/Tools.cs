// <copyright company="SIX Networks GmbH" file="Tools.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Core.Services;
using withSIX.Core.Services.Infrastructure;

namespace withSIX.Core
{
    public class ToolsServices : IDomainService
    {
        public ToolsServices(IProcessManager pm, Lazy<IWCFClient> wcfClient, 
            ICompressionUtil compression, IUacHelper uacHelper) {
            Compression = compression;
            UacHelper = uacHelper;
            ProcessManager = pm;
            WCFClient = wcfClient;
        }

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

        public static IUacHelper UacHelper { get; private set; }

        public static Func<string, string, Exception, Task> InformUserError { get; set; }

        private static ICompressionUtil _compressionUtil { get; set; }
        public static ICompressionUtil CompressionUtil => _compressionUtil;

        public static void RegisterServices(ToolsServices services) {
            ProcessManager = services.ProcessManager;
            _wcfClient = services.WCFClient;
            _compressionUtil = services.Compression;
            UacHelper = services.UacHelper;
        }
    }
}