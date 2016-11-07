// <copyright company="SIX Networks GmbH" file="IPublishingApi.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Api.Models;
using withSIX.Api.Models.Publishing;

namespace withSIX.Sync.Core.Services
{
    public interface IPublishingApi
    {
        Task<Guid> Publish(PublishModModel model);
        Task Signal();
        Task Deversion(SpecificVersion nextInline);
        Task PublishState(StateChangeMessageModel model);
    }
}