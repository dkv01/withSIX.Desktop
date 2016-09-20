﻿// <copyright company="SIX Networks GmbH" file="SyncEvilGlobal.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Services;
using SN.withSIX.Sync.Core.Legacy;
using SN.withSIX.Sync.Core.Transfer;

namespace SN.withSIX.Sync.Core
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

        public static IYamlUtil Yaml { get; private set; }

        // pff
        public static Func<int> Limiter { get; private set; }

        public static void Setup(EvilGlobalServices services, Func<int> limiter) {
            DownloadHelper = services.DownloadHelper;
            FileDownloader = services.Downloader;
            StringDownloader = services.StringDownloader;
            GetHostChecker = services.GetHostChecker;
            Limiter = limiter;
            Yaml = services.Yaml;
        }
    }

    [Obsolete(
         "This only exists because the classes using this, were not designed to work with DI in mind. This should however be rectified asap.."
     )]
    public class EvilGlobalServices : IDomainService
    {
        public EvilGlobalServices(IFileDownloadHelper downloadHelper, IFileDownloader fileDownloader,
            IStringDownloader stringDownloader,
            Func<ExportLifetimeContext<IHostChecker>> getHostChecker, IYamlUtil yaml) {
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
        public IYamlUtil Yaml { get; }
    }

    public interface IYamlUtil
    {
        Task<T> GetYaml<T>(Uri uri, string token = null);
        string ToYaml(object graph);
        void ToYamlFile(IBaseYaml graph, IAbsoluteFilePath fileName);

        //void PrintMapping(YamlMappingNode mapping);
        T NewFromYamlFile<T>(IAbsoluteFilePath fileName);
        T NewFromYaml<T>(string yaml);
    }
}