// <copyright company="SIX Networks GmbH" file="IPublishingApi.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Api.Models;
using withSIX.Api.Models.Publishing;

namespace SN.withSIX.Sync.Core.Services
{
    public interface IPublishingApi
    {
        Task<Guid> Publish(PublishModModel model, string registerKey);
        Task Signal(string registerKey);
        Task Deversion(SpecificVersion nextInline, string registerKey);
    }
}