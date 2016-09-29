// <copyright company="SIX Networks GmbH" file="ResourceService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.IO;
using System.Reflection;
using withSIX.Core.Applications.Infrastructure;
using withSIX.Core.Presentation.Resources;

namespace withSIX.Core.Presentation.Wpf.Services
{
    public class ResourceService : IPresentationResourceService, IPresentationService
    {
        const string SourceAssemblyName = "withSIX.Core.Presentation.Resources";
        readonly Assembly _sourceAssembly = typeof(DummyClass).GetTypeInfo().Assembly;

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