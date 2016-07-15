// <copyright company="SIX Networks GmbH" file="IResourceService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.IO;

namespace SN.withSIX.Core.Applications.Infrastructure
{
    public interface IResourceService
    {
        Stream GetResource(string path);
    }

    public interface IPresentationResourceService
    {
        Stream GetResource(string path);
    }
}