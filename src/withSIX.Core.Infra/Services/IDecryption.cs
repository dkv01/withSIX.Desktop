// <copyright company="SIX Networks GmbH" file="IDecryption.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;

namespace withSIX.Core.Infra.Services
{
    public interface IDecryption
    {
        Task<string> Decrypt(string encryptedPremiumToken, string apiKey);
    }
}