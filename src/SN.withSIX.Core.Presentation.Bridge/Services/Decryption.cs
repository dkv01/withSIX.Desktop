// <copyright company="SIX Networks GmbH" file="Decryption.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using Synercoding.Encryption.Hashing;
using Synercoding.Encryption.Symmetrical;
using withSIX.Core.Infra.Services;

namespace withSIX.Core.Presentation.Bridge.Services
{
    public class Decryption : IDecryption, IPresentationService
    {
        public async Task<string> Decrypt(string encryptedPremiumToken, string apiKey) {
            var aes = new Aes();
            var sha1 = new SHA1Hash();

            var keyHash = await sha1.GetHashAsync(apiKey).ConfigureAwait(false);
            var unencryptedPremiumToken =
                await aes.DecryptAsync(encryptedPremiumToken, keyHash).ConfigureAwait(false);
            return unencryptedPremiumToken;
        }

    }
}