// <copyright company="SIX Networks GmbH" file="PublishingApi.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using withSIX.Api.Models.Publishing;
using withSIX.Core;
using withSIX.Core.Helpers;
using withSIX.Core.Infra.Services;
using withSIX.Sync.Core.Services;
using withSIX.Api.Models;

namespace withSIX.Sync.Presentation.Console.Services
{
    public class PublishingApi : IPublishingApi, IInfrastructureService
    {
        public async Task<Guid> Publish(PublishModModel model, string registerKey) {
            var r = await
                Tools.Transfer.PostJson(model,
                        Tools.Transfer.JoinUri(CommonUrls.PublishApiUrl,
                            "api/v2/publishing/mods?registerKey=" + Uri.EscapeDataString(registerKey)))
                    .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<Guid>(r);
        }

        public async Task Signal(string registerKey) {
            await
                Tools.Transfer.PostJson(new {},
                        Tools.Transfer.JoinUri(CommonUrls.PublishApiUrl,
                            "api/v2/publishing/signal?registerKey=" + Uri.EscapeDataString(registerKey)))
                    .ConfigureAwait(false);
        }

        public async Task Deversion(SpecificVersion nextInline, string registerKey) {
            await
                Tools.Transfer.PostJson(new {nextInline.Name, nextInline.VersionData},
                        Tools.Transfer.JoinUri(CommonUrls.PublishApiUrl,
                            "api/v2/publishing/mods_deversion?registerKey=" + Uri.EscapeDataString(registerKey)))
                    .ConfigureAwait(false);
        }
    }
}