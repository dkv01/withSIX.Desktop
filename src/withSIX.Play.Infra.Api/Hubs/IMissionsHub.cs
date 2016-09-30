// <copyright company="SIX Networks GmbH" file="IMissionsHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using SignalRNetClientProxyMapper;

using withSIX.Api.Models;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.Arma3;

namespace withSIX.Play.Infra.Api.Hubs
{

    interface IMissionsHub : IClientHubProxyBase
    {
        Task<AWSUploadPolicy> RequestMissionUpload(RequestMissionUploadModel model);
        Task<MissionModel> MissionUploadCompleted(MissionUploadedModel model);
        Task<PageModel<MissionModel>> GetMyMissions(string type, int page);
    }

}