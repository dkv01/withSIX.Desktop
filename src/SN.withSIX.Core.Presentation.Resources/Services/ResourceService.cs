// <copyright company="SIX Networks GmbH" file="ResourceService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>


using System.IO;
using System.Reflection;
using SN.withSIX.Core.Applications.Infrastructure;

namespace SN.withSIX.Core.Presentation.Resources.Services
{
    public class ResourceService : IPresentationResourceService, IPresentationService
    {
        const string SourceAssemblyName = "SN.withSIX.Core.Presentation.Resources";
        readonly Assembly _sourceAssembly = typeof (DummyClass).Assembly;

        static ResourceService() {
            ComponentPath = "/" + SourceAssemblyName + ";component";
            ResourcePath = "pack://application:,,," + ComponentPath;
        }

        public static string ComponentPath { get; }
        public static string ResourcePath { get; }

        public Stream GetResource(string path) => _sourceAssembly.GetManifestResourceStream(GetResourcePath(path));

        static string GetResourcePath(string path) => SourceAssemblyName + "." +
                                                      path.Replace("/", ".").Replace("\\", ".");
    }
}