// <copyright company="SIX Networks GmbH" file="IFileUploader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Sync.Core.Transfer.Specs;

namespace withSIX.Sync.Core.Transfer
{
    public interface IFileUploader
    {
        void Upload(FileUploadSpec localFile);
        Task UploadAsync(FileUploadSpec spec);
    }
}