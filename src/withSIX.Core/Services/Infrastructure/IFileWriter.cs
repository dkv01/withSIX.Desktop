// <copyright company="SIX Networks GmbH" file="IFileWriter.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Text;
using System.Threading.Tasks;

namespace withSIX.Core.Services.Infrastructure
{
    public interface IFileWriter
    {
        void WriteFile(string fileName, string fileContent);
        Task WriteFileAsync(string fileName, string fileContent);
        void WriteFile(string fileName, string fileContent, Encoding encoding);
        Task WriteFileAsync(string fileName, string fileContent, Encoding encoding);
    }
}