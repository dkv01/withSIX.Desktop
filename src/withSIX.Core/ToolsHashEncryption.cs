// <copyright company="SIX Networks GmbH" file="HashEncryption.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NDepend.Path;

namespace withSIX.Core
{
    public static partial class Tools
    {
        public static HashEncryptionTools HashEncryption = new HashEncryptionTools();

        // TODO: Support progress reporting http://stackoverflow.com/questions/6493220/get-progress-of-sha1-being-computed-using-sha1cryptoserviceprovider
        public class HashEncryptionTools
        {
            public string MD5Hash(string data) {
                Contract.Requires<ArgumentNullException>(data != null);

                using (var md5 = MD5.Create())
                    return GetHash(md5.ComputeHash(Encoding.ASCII.GetBytes(data)));
            }

            string GetHash(byte[] hash) => BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

            public string MD5FileHash(IAbsoluteFilePath fileName) {
                Contract.Requires<ArgumentNullException>(fileName != null);

                return FileUtil.Ops.AddIORetryDialog(() => {
                    using (var fs = new FileStream(fileName.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read))
                        //using (var stream = new BufferedStream(fs, GetBufferSize(fs)))
                    using (var md5 = MD5.Create())
                        return GetHash(md5.ComputeHash(fs));
                }, fileName.ToString());
            }

            public string SHA1FileHash(IAbsoluteFilePath fileName) {
                Contract.Requires<ArgumentNullException>(fileName != null);

                return FileUtil.Ops.AddIORetryDialog(() => {
                    using (var fs = new FileStream(fileName.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read))
                        //using (var stream = new BufferedStream(fs, GetBufferSize(fs)))
                    using (var md5 = SHA1.Create())
                        return GetHash(md5.ComputeHash(fs));
                }, fileName.ToString());
            }

            public int GetBufferSize(FileStream fs) {
                var buffer = 1200000 > fs.Length ? (int) fs.Length : 1200000;
                return buffer > 0 ? buffer : 1;
            }

            public string SHA256FileHash(string fileName) {
                Contract.Requires<ArgumentNullException>(fileName != null);
                Contract.Requires<ArgumentOutOfRangeException>(!string.IsNullOrEmpty(fileName));

                return FileUtil.Ops.AddIORetryDialog(() => {
                    using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        //using (var stream = new BufferedStream(fs, GetBufferSize(fs)))
                    using (var md5 = SHA256.Create())
                        return GetHash(md5.ComputeHash(fs));
                }, fileName);
            }

            public string SHA256Hash(string data) {
                Contract.Requires<ArgumentNullException>(data != null);

                using (var sha = SHA256.Create())
                    return GetHash(sha.ComputeHash(Encoding.ASCII.GetBytes(data)));
            }

            public string SHA384Hash(string data) {
                Contract.Requires<ArgumentNullException>(data != null);

                using (var sha = SHA384.Create())
                    return GetHash(sha.ComputeHash(Encoding.ASCII.GetBytes(data)));
            }

            public string SHA512Hash(string data) {
                Contract.Requires<ArgumentNullException>(data != null);

                using (var sha = SHA512.Create())
                    return GetHash(sha.ComputeHash(Encoding.ASCII.GetBytes(data)));
            }
        }
    }
}