// <copyright company="SIX Networks GmbH" file="AuthInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Core.Extensions;

namespace withSIX.Sync.Core.Transfer
{
    public interface IAuthInfo
    {
        string Username { get; }
        string Password { get; }
        string Domain { get; }
    }

    [DataContract(Name = "AuthInfo", Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    public class AuthInfo : IAuthInfo
    {
        string _domain;
        [DataMember] string _domainEncoded;
        string _password;
        [DataMember] string _passwordEncoded;
        string _username;
        [DataMember] string _usernameEncoded;

        public AuthInfo(string username, string password, string domain = null) {
            Username = username;
            Password = password;
            Domain = domain;
        }

        public string Username
        {
            get
            {
                return _username ??
                       (_usernameEncoded == null ? null : (_username = _usernameEncoded.Decode64()));
            }
            set
            {
                _username = value;
                _usernameEncoded = value == null ? null : value.EncodeTo64();
            }
        }
        public string Password
        {
            get
            {
                return _password ??
                       (_passwordEncoded == null ? null : (_password = _passwordEncoded.Decode64()));
            }
            set
            {
                _password = value;
                _passwordEncoded = value == null ? null : value.EncodeTo64();
            }
        }
        public string Domain
        {
            get { return _domain ?? (_domainEncoded == null ? null : (_domain = _domainEncoded.Decode64())); }
            set
            {
                _domain = value;
                _domainEncoded = value == null ? null : value.EncodeTo64();
            }
        }
    }
}