// <copyright company="SIX Networks GmbH" file="SyncEvilGlobal.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Core.Services;
using withSIX.Sync.Core.Legacy;
using withSIX.Sync.Core.Transfer;

namespace withSIX.Sync.Core
{
    [Obsolete(
         "This only exists because the classes using this, were not designed to work with DI in mind. This should however be rectified asap.."
     )]
    public static class SyncEvilGlobal
    {
        public static IFileDownloadHelper DownloadHelper { get; private set; }
        public static IFileDownloader FileDownloader { get; private set; }
        public static IStringDownloader StringDownloader { get; private set; }
        public static Func<ExportLifetimeContext<IHostChecker>> GetHostChecker { get; private set; }
        public static IYamlUtil Yaml => YamlInternal.Value;
        static Lazy<IYamlUtil> YamlInternal { get; set; }
        public static Func<int> Limiter { get; private set; }

        public static void Setup(EvilGlobalServices services, Func<int> limiter) {
            DownloadHelper = services.DownloadHelper;
            FileDownloader = services.Downloader;
            StringDownloader = services.StringDownloader;
            GetHostChecker = services.GetHostChecker;
            Limiter = limiter;
            YamlInternal = services.Yaml;
        }
    }

    [Obsolete(
         "This only exists because the classes using this, were not designed to work with DI in mind. This should however be rectified asap.."
     )]
    public class EvilGlobalServices : IDomainService
    {
        public EvilGlobalServices(IFileDownloadHelper downloadHelper, IFileDownloader fileDownloader,
            IStringDownloader stringDownloader,
            Func<ExportLifetimeContext<IHostChecker>> getHostChecker, Lazy<IYamlUtil> yaml) {
            DownloadHelper = downloadHelper;
            Downloader = fileDownloader;
            StringDownloader = stringDownloader;
            GetHostChecker = getHostChecker;
            Yaml = yaml;
        }

        public IFileDownloader Downloader { get; }
        public IFileDownloadHelper DownloadHelper { get; }
        public Func<ExportLifetimeContext<IHostChecker>> GetHostChecker { get; }
        public IStringDownloader StringDownloader { get; }
        public Lazy<IYamlUtil> Yaml { get; }
    }

    public interface IYamlUtil
    {
        Task<T> GetYaml<T>(Uri uri, CancellationToken ct = default(CancellationToken), string token = null);
        string ToYaml(object graph);
        void ToYamlFile(IBaseYaml graph, IAbsoluteFilePath fileName);

        //void PrintMapping(YamlMappingNode mapping);
        T NewFromYamlFile<T>(IAbsoluteFilePath fileName);
        T NewFromYaml<T>(string yaml);
    }
}