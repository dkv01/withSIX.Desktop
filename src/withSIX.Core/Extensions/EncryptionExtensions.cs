// <copyright company="SIX Networks GmbH" file="EncryptionExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Core.Extensions
{
    public static class EncryptionExtensions
    {
        /*
        const DataProtectionScope DataProtectionScope = System.Security.Cryptography.DataProtectionScope.CurrentUser;
        const string EncryptionSalt = "j8RlPSi0OKfvDrbT";

        public static string Encrypt(this string str) {
            if (str == null) throw new ArgumentNullException(nameof(str));

            var byteData = Encoding.UTF8.GetBytes(str);
            var encryptionSaltByteData = Encoding.UTF8.GetBytes(EncryptionSalt);
            var encryptedByteData = ProtectedData.Protect(byteData, encryptionSaltByteData, DataProtectionScope);
            return Convert.ToBase64String(encryptedByteData);
        }

        public static string Decrypt(this string str) {
            if (str == null) throw new ArgumentNullException(nameof(str));

            if (str == string.Empty)
                return string.Empty;

            var byteData = Convert.FromBase64String(str);
            var encryptionSaltByteData = Encoding.UTF8.GetBytes(EncryptionSalt);
            var decryptedByteData = ProtectedData.Unprotect(byteData, encryptionSaltByteData, DataProtectionScope);
            return Encoding.UTF8.GetString(decryptedByteData);
        }
        */
    }
}