﻿// <copyright company="SIX Networks GmbH" file="ConnectApiHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Web;
using Amazon.S3.Util;
using AutoMapper;
using AutoMapper.Mappers;
using Microsoft.AspNet.SignalR.Client;
using NDepend.Path;
using ReactiveUI;
using ShortBus;
using withSIX.Api.Models;
using withSIX.Api.Models.Collections;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.Arma3;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Social;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Play.Applications;
using SN.withSIX.Play.Core;
using SN.withSIX.Play.Core.Connect;
using SN.withSIX.Play.Core.Connect.Infrastructure;
using SN.withSIX.Play.Core.Options;
using SN.withSIX.Play.Infra.Api.Hubs;
using withSIX.Api.Models.Content.v2;

namespace SN.withSIX.Play.Infra.Api.ConnectApi
{
    class AvatarCalc
    {
        public static string GetAvatarURL(AccountInfo account) => GetAvatarURL(account, 72);

        public static string GetAvatarURL(AccountInfo account, int size) => account.AvatarURL == null
    ? GetGravatarUrl(account.EmailMd5, size)
    : GetCustomAvatarUrl(account.AvatarURL, account.AvatarUpdatedAt.GetValueOrDefault(0), size);

        public static string GetAvatarURL(string avatarUrl, long? avatarUpdatedAt, string emailMd5, int size = 72) => avatarUrl == null
    ? GetGravatarUrl(emailMd5, size)
    : GetCustomAvatarUrl(avatarUrl, avatarUpdatedAt.GetValueOrDefault(0), size);

        static string GetGravatarUrl(string emailMd5, int size) => "//www.gravatar.com/avatar/" + emailMd5 +
       "?size=" + size + "&amp;d=%2f%2faz667488.vo.msecnd.net%2fimg%2favatar%2fnoava_" +
       size + ".jpg";

        static string GetCustomAvatarUrl(string avatarUrl, long avatarUpdatedAt, int size) {
            var v = "?v=" + avatarUpdatedAt;
            return avatarUrl + size + "x" + size + ".jpg" + v;
        }
    }

    class ConnectApiHandler : PropertyChangedBase, IInfrastructureService, IConnectApiHandler
    {
        static readonly string defaultAvaImg =
            HttpUtility.UrlEncode("http://withsix-assets.s3-eu-west-1.amazonaws.com/img/avatar/placeholder_40.png");
        static readonly DataAnnotationsValidator.DataAnnotationsValidator validator =
            new DataAnnotationsValidator.DataAnnotationsValidator();
        readonly IConnectionManager _connectionManager;
        readonly IExceptionHandler _exHandler;
        readonly IMapper _mappingEngine;
        readonly ILoginHandler _loginHandler;

        public ConnectApiHandler(IConnectionManager connectionManager, ILoginHandler loginHandler, IExceptionHandler exHandler) {
            _connectionManager = connectionManager;
            _loginHandler = loginHandler;
            _exHandler = exHandler;
            Me = new MyAccount();
            _mappingEngine = GetMapper();
            SetupListeners();
        }

        public async Task Login() {
            await _loginHandler.ProcessLogin().ConfigureAwait(false);
            UpdateAccount(MapAccount(DomainEvilGlobal.SecretData.UserInfo.Account));
        }

        public IMessageBus MessageBus => _connectionManager.MessageBus;

        public async Task<ConnectionScoper> StartSession() {
            var accessToken = DomainEvilGlobal.SecretData.UserInfo.AccessToken;
            await _connectionManager.Start(accessToken).ConfigureAwait(false);
            return new ConnectionScoper(_connectionManager);
        }

        public async Task<CollectionModel> GetCollection(Guid collectionId) {
            ConfirmConnected();
            return await _connectionManager.CollectionsHub.GetCollection(collectionId).ConfigureAwait(false);
        }

        public async Task<CollectionVersionModel> GetCollectionVersion(Guid versionId) {
            ConfirmConnected();
            return await _connectionManager.CollectionsHub.GetCollectionVersion(versionId).ConfigureAwait(false);
        }

        public async Task<CollectionPublishInfo> PublishCollection(CreateCollectionModel model) {
            ValidateObject(model);
            ConfirmConnected();

            var accountId = DomainEvilGlobal.SecretData.UserInfo.Account.Id;
            var id = await _connectionManager.CollectionsHub.CreateNewCollection(model).ConfigureAwait(false);
            return new CollectionPublishInfo(id, accountId);
        }

        public async Task<Guid> PublishNewCollectionVersion(AddCollectionVersionModel model) {
            ValidateObject(model);
            ConfirmConnected();
            return await _connectionManager.CollectionsHub.AddCollectionVersion(model).ConfigureAwait(false);
        }

        public async Task<List<CollectionModel>> GetSubscribedCollections() {
            ConfirmConnected();
            return await _connectionManager.CollectionsHub.GetSubscribedCollections().ConfigureAwait(false);
        }

        public async Task<List<CollectionModel>> GetOwnedCollections() {
            ConfirmConnected();
            return await _connectionManager.CollectionsHub.GetOwnedCollections().ConfigureAwait(false);
        }

        public async Task UnsubscribeCollection(Guid collectionID) {
            ConfirmConnected();
            await _connectionManager.CollectionsHub.Unsubscribe(collectionID).ConfigureAwait(false);
        }

        public async Task DeleteCollection(Guid collectionId) {
            ConfirmConnected();
            await _connectionManager.CollectionsHub.Delete(collectionId).ConfigureAwait(false);
            }

        public async Task ChangeCollectionScope(Guid collectionId, CollectionScope scope) {
            ConfirmConnected();
            await _connectionManager.CollectionsHub.ChangeScope(collectionId, scope).ConfigureAwait(false);
        }

        public async Task ChangeCollectionName(Guid collectionId, string name) {
            ConfirmConnected();
            await _connectionManager.CollectionsHub.UpdateCollectionName(collectionId, name).ConfigureAwait(false);
        }

        public async Task<string> GenerateNewCollectionImage(Guid id) {
            ConfirmConnected();
            return await _connectionManager.CollectionsHub.GenerateNewAvatarImage(id).ConfigureAwait(false);
        }

        public async Task UploadMission(RequestMissionUploadModel model, IAbsoluteDirectoryPath path) {
            Contract.Requires<ArgumentNullException>(model != null);
            Contract.Requires<ArgumentNullException>(path != null);

            ValidateObject(model);
            ConfirmConnected();
            var uploadRequest = await _connectionManager.MissionsHub.RequestMissionUpload(model).ConfigureAwait(false);
            await UploadFileToAWS(path.GetChildFileWithName(model.FileName), uploadRequest).ConfigureAwait(false);
            var uploadedModel = new MissionUploadedModel {
                GameSlug = model.GameSlug,
                Name = model.Name,
                UploadKey = uploadRequest.Key
            };
            ValidateObject(uploadedModel);
            await _connectionManager.MissionsHub.MissionUploadCompleted(uploadedModel).ConfigureAwait(false);
        }

        public async Task<PageModel<MissionModel>> GetMyMissions(string type, int page) {
            ConfirmConnected();
            return await _connectionManager.MissionsHub.GetMyMissions(type, page).ConfigureAwait(false);
        }

        public MyAccount Me { get; }

        public void ConfirmLoggedIn() {
            if (!_connectionManager.IsLoggedIn())
                throw new NotLoggedInException();
        }

        public async Task<string> UploadCollectionAvatar(IAbsoluteFilePath imagePath, Guid collectionId) {
            ConfirmConnected();
            var uploadRequest =
                await
                    _connectionManager.CollectionsHub.RequestAvatarUpload(imagePath.ToString(), collectionId)
                        .ConfigureAwait(false);
            await UploadFileToAWS(imagePath, uploadRequest);
            return await
                _connectionManager.CollectionsHub.AvatarUploadCompleted(collectionId, uploadRequest.Key)
                    .ConfigureAwait(false);
        }

        void ConfirmConnected() {
            if (!_connectionManager.IsLoggedIn() || !_connectionManager.IsConnected())
                throw new NotConnectedException();
        }

        // ReSharper disable once InconsistentNaming
        Task UploadFileToAWS(IAbsoluteFilePath filePath, AWSUploadPolicy uploadRequest) => UploadToAWS(uploadRequest, File.OpenRead(filePath.ToString()));

        // ReSharper disable once InconsistentNaming
        async Task UploadToAWS(AWSUploadPolicy uploadRequest, FileStream inputStream) {
            var s3PostUploadSignedPolicy =
                S3PostUploadSignedPolicy.GetSignedPolicyFromJson(uploadRequest.EncryptedPolicy);
            s3PostUploadSignedPolicy.SecurityToken = uploadRequest.SecurityToken;
            var uploadResponse = await Task.Run(() => AmazonS3Util.PostUpload(new S3PostUploadRequest {
                Key = uploadRequest.Key,
                Bucket = uploadRequest.BucketName,
                CannedACL = uploadRequest.ACL,
                ContentType = uploadRequest.ContentType,
                SuccessActionRedirect = uploadRequest.CallbackUrl,
                InputStream = inputStream,
                SignedPolicy = s3PostUploadSignedPolicy
            })).ConfigureAwait(false);
            if (uploadResponse.StatusCode != HttpStatusCode.OK)
                throw new Exception("Amazon upload failed: " + uploadResponse.StatusCode);
        }

        static void ValidateObject(object model) {
            validator.ValidateObject(model);
        }

        void SetupListeners() {
            Listen<ApiHashes>(ApiHashesReceived);
            Listen<SubscribedToCollection>(SubscribedToCollection);
            Listen<UnsubscribedFromCollection>(UnsubscribeFromCollection);
            Listen<CollectionUpdated>(CollectionUpdated);
            Listen<CollectionVersionAdded>(CollectionVersionAdded);
        }

        async Task ApiHashesReceived(ApiHashes obj) {
            await Cheat.PublishDomainEvent(obj).ConfigureAwait(false);
        }

        void Listen<TEvt>(Func<TEvt, Task> action) {
            _connectionManager.MessageBus.Listen<TEvt>().Subscribe(x => HandleAction(action, x));
        }

        async void HandleAction<TEvt>(Func<TEvt, Task> action, TEvt x) {
            retry:
            try {
                await action(x).ConfigureAwait(false);
            } catch (Exception ex) {
                var r = await UserError.Throw(_exHandler.HandleException(ex, "API action"));
                if (r == RecoveryOptionResult.RetryOperation)
                    goto retry;
                if (r == RecoveryOptionResult.FailOperation)
                    throw;
            }
        }

        async Task CollectionUpdated(CollectionUpdated evt) {
            await Cheat.PublishDomainEvent(evt).ConfigureAwait(false);
        }

        async Task CollectionVersionAdded(CollectionVersionAdded evt) {
            await Cheat.PublishDomainEvent(evt).ConfigureAwait(false);
        }

        async Task UnsubscribeFromCollection(UnsubscribedFromCollection evt) {
            await Cheat.PublishDomainEvent(evt).ConfigureAwait(false);
        }

        async Task SubscribedToCollection(SubscribedToCollection evt) {
            await Cheat.PublishDomainEvent(evt).ConfigureAwait(false);
        }

        void UpdateAccount(MyAccountModel myAccount) {
            Me.Account = myAccount.Account;
        }

        MyAccountModel MapAccount(AccountInfo accountInfo) {
            var myAccountModel = new MyAccountModel {
                Account = _mappingEngine.Map<Account>(accountInfo)
                //Friends = await GetFriends(context).ConfigureAwait(false),
                //Groups = await GetGroups().ConfigureAwait(false),
                //InviteRequests = await GetInviteRequests().ConfigureAwait(false)
            };
            return myAccountModel;
        }

        IMapper GetMapper() {
            var c = new MapperConfiguration(mapConfig => {
                mapConfig.SetupConverters();

                mapConfig.CreateMap<AccountInfo, Account>()
                    .ForMember(x => x.Avatar,
                        opt => opt.MapFrom(src => new Uri("http:" + AvatarCalc.GetAvatarURL(src))))
                    .ForMember(x => x.Slug, opt => opt.MapFrom(src => src.UserName.Sluggify()));
            });

            return c.CreateMapper();
        }

        static string GetAvatar(AccountModel src) => String.IsNullOrWhiteSpace(src.AvatarUrl)
    ? GetGravatar(src.EmailMd5)
    : GetAvatarUrl(src.AvatarUrl, src.AvatarUpdatedAt);

        static string GetAvatar(GroupModel src) => String.IsNullOrWhiteSpace(src.AvatarUrl) || src.AvatarUrl.EndsWith("/")
    ? null
    : AddDefaultProtocol(src.AvatarUrl) + GetQs(src.AvatarUpdatedAt);

        static string GetBackgroundUrl(GroupModel src) => String.IsNullOrWhiteSpace(src.BackgroundUrl) || src.BackgroundUrl.EndsWith("/")
    ? null
    : AddDefaultProtocol(src.BackgroundUrl) + GetQs(src.BackgroundUpdatedAt);

        static string GetAvatarUrl(string avatarUrl, DateTime? updatedAt) => String.IsNullOrWhiteSpace(avatarUrl)
    ? null
    : (AddDefaultProtocol(avatarUrl) + "72x72.jpg" + GetQs(updatedAt));

        static string GetQs(DateTime? updatedAt) => "?v=" + updatedAt.GetValueOrDefault().GetStamp();

        static string AddDefaultProtocol(string avatarUrl) => "http:" + avatarUrl;

        static string GetGravatar(string md5) => "http://www.gravatar.com/avatar/" + md5 + "?size=72&d=" + defaultAvaImg;
    }
}