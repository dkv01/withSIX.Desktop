// <copyright company="SIX Networks GmbH" file="Settings.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using withSIX.Api.Models.Premium;
using SN.withSIX.Core;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core;
using SN.withSIX.Sync.Core.Legacy.Status;
using SN.withSIX.Sync.Core.Transfer;

namespace SN.withSIX.Mini.Applications.Models
{
    public class Settings
    {
        [IgnoreDataMember] readonly IDomainEventHandler _domainEventHandler = CoreCheat.EventGrabber.GetSettings();

        public Settings() {
            Roaming = new RoamingSettings();
            Secure = new SecureSettings();
            Local = new LocalSettings();
        }

        public Settings(RoamingSettings roaming, SecureSettings secure, LocalSettings local) {
            Roaming = roaming;
            Secure = secure;
            Local = local;
        }

        public RoamingSettings Roaming { get; protected set; }
        public SecureSettings Secure { get; protected set; }
        public LocalSettings Local { get; protected set; }

        protected void PrepareEvent(ISyncDomainEvent evt) => _domainEventHandler.PrepareEvent(this, evt);

        public void UpdateLogin(LoginInfo login) {
            Secure.Login = login;
            PrepareEvent(new LoginChanged(Secure.Login));
        }

        public void UpdateSteamCredentials(Credentials creds) {
            Secure.SteamCredentials = creds;
        }

        public void ExtensionInstalled() => UpdateInstalledState(true);
        public void ExtensionUninstalled() => UpdateInstalledState(false);

        private void UpdateInstalledState(bool state) {
            Local.InstalledExtension = state;
            PrepareEvent(new ExtensionStateChanged(state));
        }

        public int ApiPort => Local.ApiPort.GetValueOrDefault(Consts.DefaultHttpsPort);

        public void Mapped() {
            Common.Flags.Verbose = Local.EnableDiagnosticsMode || Common.Flags.OriginalVerbose;
        }
    }

    public class ExtensionStateChanged : ISyncDomainEvent
    {
        public ExtensionStateChanged(bool state) {
            State = state;
        }

        public bool State { get; }
    }

    public static class PremiumTokenExtensions
    {
        public static bool IsPremium(this PremiumAccessTokenV1 token) => token != null &&
                                                                         token.PremiumUntil >
                                                                         Tools.Generic.GetCurrentUtcDateTime;

        public static bool IsValidInNearFuture(this PremiumAccessTokenV1 token)
            => token != null && token.ValidUntil > Tools.Generic.GetCurrentUtcDateTime.AddHours(6);
    }

    [DataContract]
    public class LoginInfo
    {
        public static readonly Uri DefaultAvatarUrl =
            new Uri("http://www.vacul.org/extension/site/design/site/images/anonymous-user.png");
        public static readonly LoginInfo Default = new LoginInfo();
        LoginInfo() : this(new AccountInfo(), new AuthenticationInfo()) {}

        protected LoginInfo(AccountInfo accountInfo, AuthenticationInfo authInfo) {
            Account = accountInfo;
            Authentication = authInfo;
        }

        public bool IsPremium => Authentication.PremiumToken.IsPremium();
        [DataMember]
        public AccountInfo Account { get; protected set; }
        [DataMember]
        public AuthenticationInfo Authentication { get; protected set; }
        [DataMember]
        public virtual bool IsLoggedIn { get; protected set; }
    }

    // TODO: This is not properly serialized as LoggedInInfo, thats why the Overlay doesnt work properly
    // thats why we set the IsLoggedIN and have a setter :(
    [DataContract]
    public class LoggedInInfo : LoginInfo
    {
        public LoggedInInfo(AccountInfo accountInfo, AuthenticationInfo authInfo) : base(accountInfo, authInfo) {}
        [DataMember]
        public override bool IsLoggedIn { get; protected set; } = true;
    }

    [DataContract]
    public class SecureSettings
    {
        public SecureSettings() {}

        protected SecureSettings(LoginInfo login, Guid clientId) {
            Login = login;
            ClientId = clientId;
        }

        [DataMember]
        public LoginInfo Login { get; protected internal set; }
        [DataMember]
        public Guid ClientId { get; protected set; } = Guid.NewGuid();
        [DataMember]
        public Credentials SteamCredentials { get; protected internal set; }
    }

    [DataContract]
    public class Credentials : IAuthInfo
    {
        public Credentials(string username, string password, string domain = null) {
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(username));
            Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(password));
            Username = username;
            Password = password;
            Domain = domain;
        }

        [DataMember]
        public string Username { get;}
        [DataMember]
        public string Password { get;}
        [DataMember]
        public string Domain { get;}
    }


    [DataContract]
    public class RoamingSettings {}

    [DataContract]
    public class LocalSettings
    {
        //[DataMember]
        //public Guid SelectedGameId { get; set; }
        [DataMember]
        public bool OptOutReporting { get; set; }
        [DataMember]
        public bool ShowDesktopNotifications { get; set; } = true;
        [DataMember]
        public bool UseSystemBrowser { get; set; } = true;
        [DataMember]
        public bool StartWithWindows { get; set; } = true;
        [DataMember]
        public string CurrentVersion { get; set; }
        [DataMember]
        public int PlayWithSixImportVersion { get; set; }
        [DataMember]
        public bool DeclinedPlaywithSixImport { get; set; }
        [DataMember]
        public bool EnableDiagnosticsMode { get; set; }
        [DataMember]
        public bool InstalledExtension { get; protected internal set; }
        [DataMember]
        public int? MaxConcurrentDownloads { get; set; }
        [DataMember]
        public int? ApiPort { get; set; }
    }

    public class PremiumAccessToken : PremiumAccessTokenV1, IEquatable<PremiumAccessToken>
    {
        public PremiumAccessToken(string accessToken, string premiumKey) : base(accessToken, premiumKey) {}

        public bool Equals(PremiumAccessToken other) {
            if (ReferenceEquals(this, other))
                return true;
            if (other == null)
                return false;
            return other.AccessToken == AccessToken && other.PremiumKey == PremiumKey;
        }

        public override int GetHashCode() => HashCode.Start.Hash(AccessToken).Hash(PremiumKey);

        public override bool Equals(object obj) => Equals(obj as PremiumAccessToken);
    }

    [DataContract]
    public class AuthenticationInfo
    {
        [DataMember]
        public PremiumAccessToken PremiumToken { get; set; }
        [DataMember]
        public string AccessToken { get; set; }
        [DataMember]
        public string RefreshToken { get; set; }
    }

    [DataContract]
    public class AccountInfo
    {
        [DataMember]
        public Guid Id { get; set; }
        [DataMember]
        public string DisplayName { get; set; }
        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public string AvatarURL { get; set; }
        [DataMember]
        public bool HasAvatar { get; set; }
        [DataMember]
        public string EmailMd5 { get; set; }
        [DataMember]
        public List<string> Roles { get; set; }
        [DataMember]
        public long? AvatarUpdatedAt { get; set; }
    }
}