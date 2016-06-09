// <copyright company="SIX Networks GmbH" file="HashTool.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.IO;
using System.Security.Cryptography;

namespace SN.withSIX.Core.Presentation.Assemblies
{
    public class HashTool
    {
        public static string SHA1FileHash(string fileName) {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var bufferedStream = GetBufferedStream(fs))
                return SHA1StreamHash(bufferedStream);
        }

        public static string SHA1StreamHash(BufferedStream stream) {
            using (var md5 = new SHA1CryptoServiceProvider())
                return GetHash(md5.ComputeHash(stream));
        }

        public static BufferedStream GetBufferedStream(Stream stream)
            => new BufferedStream(stream, GetBufferSize(stream));

        static string GetHash(byte[] hash) => BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

        static int GetBufferSize(Stream fs) {
            var buffer = 1200000 > fs.Length ? (int) fs.Length : 1200000;
            return buffer > 0 ? buffer : 1;
        }
    }
}