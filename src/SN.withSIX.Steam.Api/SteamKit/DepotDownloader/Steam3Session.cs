// <copyright company="SIX Networks GmbH" file="Steam3Session.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SteamKit2;
using withSIX.Api.Models.Exceptions;

namespace SN.withSIX.Steam.Api.SteamKit.DepotDownloader
{
    class Steam3Session
    {
        public delegate bool WaitCondition();

        static readonly TimeSpan STEAM3_TIMEOUT = TimeSpan.FromSeconds(30);

        readonly bool authenticatedUser;

        readonly CallbackManager callbacks;

        // output
        readonly Credentials credentials;

        // input
        readonly SteamUser.LogOnDetails logonDetails;
        readonly SteamApps steamApps;
        bool bAborted;
        bool bConnected;
        bool bConnecting;
        DateTime connectTime;
        int seq; // more hack fixes

        public SteamClient steamClient;
        public SteamUser steamUser;


        public Steam3Session(SteamUser.LogOnDetails details) {
            logonDetails = details;
            authenticatedUser = details.Username != null;
            credentials = new Credentials();
            bConnected = false;
            bConnecting = false;
            bAborted = false;
            seq = 0;

            AppTickets = new Dictionary<uint, byte[]>();
            AppTokens = new Dictionary<uint, ulong>();
            DepotKeys = new Dictionary<uint, byte[]>();
            CDNAuthTokens = new Dictionary<Tuple<uint, string>, SteamApps.CDNAuthTokenCallback>();
            AppInfo = new Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo>();
            PackageInfo = new Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo>();

            steamClient = new SteamClient();

            steamUser = steamClient.GetHandler<SteamUser>();
            steamApps = steamClient.GetHandler<SteamApps>();

            callbacks = new CallbackManager(steamClient);

            callbacks.Subscribe<SteamClient.ConnectedCallback>(ConnectedCallback);
            callbacks.Subscribe<SteamClient.DisconnectedCallback>(DisconnectedCallback);
            callbacks.Subscribe<SteamUser.LoggedOnCallback>(LogOnCallback);
            callbacks.Subscribe<SteamUser.SessionTokenCallback>(SessionTokenCallback);
            callbacks.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);
            callbacks.Subscribe<SteamUser.UpdateMachineAuthCallback>(UpdateMachineAuthCallback);

            ContentDownloader.Log("Connecting to Steam3...");

            if (authenticatedUser) {
                var fi = new FileInfo($"{logonDetails.Username}.sentryFile");
                if ((ConfigStore.TheConfig.SentryData != null) &&
                    ConfigStore.TheConfig.SentryData.ContainsKey(logonDetails.Username))
                    logonDetails.SentryFileHash = Util.SHAHash(ConfigStore.TheConfig.SentryData[logonDetails.Username]);
                else if (fi.Exists && (fi.Length > 0)) {
                    var sentryData = File.ReadAllBytes(fi.FullName);
                    logonDetails.SentryFileHash = Util.SHAHash(sentryData);
                    ConfigStore.TheConfig.SentryData[logonDetails.Username] = sentryData;
                    ConfigStore.Save();
                }
            }

            Connect();
        }

        public ReadOnlyCollection<SteamApps.LicenseListCallback.License> Licenses { get; private set; }

        public Dictionary<uint, byte[]> AppTickets { get; }
        public Dictionary<uint, ulong> AppTokens { get; }
        public Dictionary<uint, byte[]> DepotKeys { get; }
        public Dictionary<Tuple<uint, string>, SteamApps.CDNAuthTokenCallback> CDNAuthTokens { get; }
        public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> AppInfo { get; }
        public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> PackageInfo { get; }

        public uint CellID => logonDetails.CellID;

        public bool WaitUntilCallback(Action submitter, WaitCondition waiter) {
            while (!bAborted && !waiter()) {
                submitter();

                var seq = this.seq;
                do {
                    WaitForCallbacks();
                } while (!bAborted && (this.seq == seq) && !waiter());
            }

            return bAborted;
        }

        public Credentials WaitForCredentials() {
            if (credentials.IsValid || bAborted)
                return credentials;

            WaitUntilCallback(() => { }, () => credentials.IsValid);

            return credentials;
        }

        public void RequestAppInfo(uint appId) {
            if (AppInfo.ContainsKey(appId) || bAborted)
                return;

            var completed = false;
            Action<SteamApps.PICSTokensCallback> cbMethodTokens = appTokens => {
                completed = true;
                if (appTokens.AppTokensDenied.Contains(appId))
                    throw new ForbiddenException($"Insufficient privileges to get access token for app {appId}");

                foreach (var token_dict in appTokens.AppTokens)
                    AppTokens.Add(token_dict.Key, token_dict.Value);
            };

            WaitUntilCallback(
                () => {
                    callbacks.Subscribe(steamApps.PICSGetAccessTokens(new List<uint> {appId}, new List<uint>()),
                        cbMethodTokens);
                }, () => completed);

            completed = false;
            Action<SteamApps.PICSProductInfoCallback> cbMethod = appInfo => {
                completed = !appInfo.ResponsePending;

                foreach (var app_value in appInfo.Apps) {
                    var app = app_value.Value;

                    ContentDownloader.Log($"Got AppInfo for {app.ID}");
                    AppInfo.Add(app.ID, app);
                }

                foreach (var app in appInfo.UnknownApps)
                    AppInfo.Add(app, null);
            };

            var request = new SteamApps.PICSRequest(appId);
            if (AppTokens.ContainsKey(appId)) {
                request.AccessToken = AppTokens[appId];
                request.Public = false;
            }

            WaitUntilCallback(
                () => {
                    callbacks.Subscribe(
                        steamApps.PICSGetProductInfo(new List<SteamApps.PICSRequest> {request},
                            new List<SteamApps.PICSRequest>()), cbMethod);
                }, () => { return completed; });
        }

        public void RequestPackageInfo(IEnumerable<uint> packageIds) {
            var packages = packageIds.ToList();
            packages.RemoveAll(pid => PackageInfo.ContainsKey(pid));

            if ((packages.Count == 0) || bAborted)
                return;

            var completed = false;
            Action<SteamApps.PICSProductInfoCallback> cbMethod = packageInfo => {
                completed = !packageInfo.ResponsePending;

                foreach (var package_value in packageInfo.Packages) {
                    var package = package_value.Value;
                    PackageInfo.Add(package.ID, package);
                }

                foreach (var package in packageInfo.UnknownPackages)
                    PackageInfo.Add(package, null);
            };

            WaitUntilCallback(
                () => { callbacks.Subscribe(steamApps.PICSGetProductInfo(new List<uint>(), packages), cbMethod); },
                () => { return completed; });
        }

        public void RequestAppTicket(uint appId) {
            if (AppTickets.ContainsKey(appId) || bAborted)
                return;


            if (!authenticatedUser) {
                AppTickets[appId] = null;
                return;
            }

            var completed = false;
            Action<SteamApps.AppOwnershipTicketCallback> cbMethod = appTicket => {
                completed = true;

                if (appTicket.Result != EResult.OK) {
                    ContentDownloader.Log($"Unable to get appticket for {appTicket.AppID}: {appTicket.Result}");
                    Abort();
                } else {
                    ContentDownloader.Log($"Got appticket for {appTicket.AppID}!");
                    AppTickets[appTicket.AppID] = appTicket.Ticket;
                }
            };

            WaitUntilCallback(() => { callbacks.Subscribe(steamApps.GetAppOwnershipTicket(appId), cbMethod); },
                () => completed);
        }

        public void RequestDepotKey(uint depotId, uint appid = 0) {
            if (DepotKeys.ContainsKey(depotId) || bAborted)
                return;

            var completed = false;

            Action<SteamApps.DepotKeyCallback> cbMethod = depotKey => {
                completed = true;
                ContentDownloader.Log($"Got depot key for {depotKey.DepotID} result: {depotKey.Result}");

                if (depotKey.Result != EResult.OK) {
                    Abort();
                    return;
                }

                DepotKeys[depotKey.DepotID] = depotKey.DepotKey;
            };

            WaitUntilCallback(
                () => { callbacks.Subscribe(steamApps.GetDepotDecryptionKey(depotId, appid), cbMethod); },
                () => completed);
        }

        public void RequestCDNAuthToken(uint depotid, string host) {
            if (CDNAuthTokens.ContainsKey(Tuple.Create(depotid, host)) || bAborted)
                return;

            var completed = false;
            Action<SteamApps.CDNAuthTokenCallback> cbMethod = cdnAuth => {
                completed = true;
                ContentDownloader.Log(
                    $"Got CDN auth token for {host} result: {cdnAuth.Result} (expires {cdnAuth.Expiration})");

                if (cdnAuth.Result != EResult.OK) {
                    Abort();
                    return;
                }

                CDNAuthTokens[Tuple.Create(depotid, host)] = cdnAuth;
            };

            WaitUntilCallback(() => { callbacks.Subscribe(steamApps.GetCDNAuthToken(depotid, host), cbMethod); },
                () => completed);
        }

        void Connect() {
            bAborted = false;
            bConnected = false;
            bConnecting = true;
            connectTime = DateTime.Now;
            steamClient.Connect();
        }

        private void Abort(bool sendLogOff = true) {
            Disconnect(sendLogOff);
        }

        public void Disconnect(bool sendLogOff = true) {
            if (sendLogOff)
                steamUser.LogOff();

            steamClient.Disconnect();
            bConnected = false;
            bConnecting = false;
            bAborted = true;

            // flush callbacks
            callbacks.RunCallbacks();
        }


        private void WaitForCallbacks() {
            callbacks.RunWaitCallbacks(TimeSpan.FromSeconds(1));

            var diff = DateTime.Now - connectTime;

            if ((diff > STEAM3_TIMEOUT) && !bConnected) {
                ContentDownloader.Log("Timeout connecting to Steam3.");
                Abort();
            }
        }

        private void ConnectedCallback(SteamClient.ConnectedCallback connected) {
            ContentDownloader.Log(" Done!");
            bConnecting = false;
            bConnected = true;
            if (!authenticatedUser) {
                ContentDownloader.Log("Logging anonymously into Steam3...");
                steamUser.LogOnAnonymous();
            } else {
                ContentDownloader.Log($"Logging '{logonDetails.Username}' into Steam3...");
                steamUser.LogOn(logonDetails);
            }
        }

        private void DisconnectedCallback(SteamClient.DisconnectedCallback disconnected) {
            if ((!bConnected && !bConnecting) || bAborted)
                return;

            ContentDownloader.Log("Reconnecting");
            steamClient.Connect();
        }

        private void LogOnCallback(SteamUser.LoggedOnCallback loggedOn) {
            var isSteamGuard = loggedOn.Result == EResult.AccountLogonDenied;
            var is2FA = loggedOn.Result == EResult.AccountLoginDeniedNeedTwoFactor;

            if (isSteamGuard || is2FA) {
                ContentDownloader.Log("This account is protected by Steam Guard.");

                Abort(false);

                if (is2FA) {
                    ContentDownloader.Log("Please enter your 2 factor auth code from your authenticator app: ");
                    logonDetails.TwoFactorCode = ContentDownloader.GetDetails("2fa authcode").Result;
                } else {
                    ContentDownloader.Log("Please enter the authentication code sent to your email address: ");
                    logonDetails.AuthCode = ContentDownloader.GetDetails("authcode").Result;
                }

                ContentDownloader.Log("Retrying Steam3 connection...");
                Connect();

                return;
            }
            if (loggedOn.Result == EResult.ServiceUnavailable) {
                ContentDownloader.Log($"Unable to login to Steam3: {loggedOn.Result}");
                Abort(false);

                return;
            }
            if (loggedOn.Result != EResult.OK) {
                ContentDownloader.Log($"Unable to login to Steam3: {loggedOn.Result}");
                Abort();

                return;
            }

            ContentDownloader.Log("Done!");

            seq++;
            credentials.LoggedOn = true;

            if (logonDetails.CellID == 0) {
                ContentDownloader.Log($"Using Steam3 suggested CellID: {loggedOn.CellID}");
                logonDetails.CellID = loggedOn.CellID;
            }
        }

        private void SessionTokenCallback(SteamUser.SessionTokenCallback sessionToken) {
            ContentDownloader.Log("Got session token!");
            credentials.SessionToken = sessionToken.SessionToken;
        }

        private void LicenseListCallback(SteamApps.LicenseListCallback licenseList) {
            if (licenseList.Result != EResult.OK) {
                ContentDownloader.Log($"Unable to get license list: {licenseList.Result} ");
                Abort();

                return;
            }

            ContentDownloader.Log($"Got {licenseList.LicenseList.Count} licenses for account!");
            Licenses = licenseList.LicenseList;

            var licenseQuery = Licenses.Select(lic => lic.PackageID);

            ContentDownloader.Log($"Licenses: {string.Join(", ", licenseQuery)}");
        }

        private void UpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback machineAuth) {
            var hash = Util.SHAHash(machineAuth.Data);
            ContentDownloader.Log(
                $"Got Machine Auth: {machineAuth.FileName} {machineAuth.Offset} {machineAuth.BytesToWrite} {machineAuth.Data.Length}");

            ConfigStore.TheConfig.SentryData[logonDetails.Username] = machineAuth.Data;
            ConfigStore.Save();

            var authResponse = new SteamUser.MachineAuthDetails {
                BytesWritten = machineAuth.BytesToWrite,
                FileName = machineAuth.FileName,
                FileSize = machineAuth.BytesToWrite,
                Offset = machineAuth.Offset,
                SentryFileHash = hash, // should be the sha1 hash of the sentry file we just wrote

                OneTimePassword = machineAuth.OneTimePassword,
                // not sure on this one yet, since we've had no examples of steam using OTPs

                LastError = 0, // result from win32 GetLastError
                Result = EResult.OK, // if everything went okay, otherwise ~who knows~

                JobID = machineAuth.JobID // so we respond to the correct server job
            };

            // send off our response
            steamUser.SendMachineAuthResponse(authResponse);
        }

        public class Credentials
        {
            public bool LoggedOn { get; set; }
            public ulong SessionToken { get; set; }

            public bool IsValid
            {
                get { return LoggedOn; }
            }
        }
    }
}