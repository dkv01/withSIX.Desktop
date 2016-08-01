// <copyright company="SIX Networks GmbH" file="IConnectMissionsApi.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Content.Arma3;

namespace SN.withSIX.Play.Core.Connect.Infrastructure.Components
{
    public interface IConnectMissionsApi
    {
        Task<PageModel<MissionModel>> GetMyMissions(string type, int page);
        Task UploadMission(RequestMissionUploadModel model, IAbsoluteDirectoryPath path);
    }
}