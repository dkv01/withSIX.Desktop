// <copyright company="SIX Networks GmbH" file="SIHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Extensions;
using withSIX.Mini.Applications.Features;
using withSIX.Mini.Applications.Features.Main;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Presentation.Core
{
    public class SIHandler : IUsecaseExecutor
    {
        private readonly Excecutor _executor = new Excecutor();

        public static Func<string, Dictionary<string, string[]>> ParseQueryString { get; set; }

        public async Task<object> HandleSingleInstanceCall(List<string> parameters) {
            foreach (var p in parameters.Where(IsSyncWsUrl)) {
                var uri = new Uri(p);
                await ProcessSyncWsUrl(uri).ConfigureAwait(false);
            }
            return null;
        }

        private async Task ProcessSyncWsUrl(Uri uri) {
            var details = ParseQueryString(uri.Query);
            var s = details["task"];
            var tasks = new[] {"launch"};
            if (s != null)
                tasks = s.First().Split(',');
            if (!tasks.Any()) {
                tasks = new[] {"launch"};
            }

            var gameId = GetGuid(uri.Host);
            var contentId = GetGuid(details["content"].First());
            foreach (var t in tasks) {
                switch (t) {
                case "install": {
                    await
                        RequestAsyncExecutor(new InstallContent(gameId, new ContentGuidSpec(contentId)))
                            .ConfigureAwait(false);
                    break;
                }
                /*
        case "uninstall": {
            await
                RequestAsyncExecutor(new UninstallContent(gameId, new ContentGuidSpec(contentId)))
                    .ConfigureAwait(false);
            break;
        }*/
                case "launch": {
                    await
                        RequestAsyncExecutor(new LaunchContent(gameId, new ContentGuidSpec(contentId)))
                            .ConfigureAwait(false);
                    break;
                }
                default: {
                    throw new NotSupportedException("Unknown task: " + t);
                }
                }
            }
        }

        private static Guid GetGuid(string input) {
            Guid output;
            return Guid.TryParse(input, out output) ? output : ShortGuid.Parse(input);
        }

        private Task<TResponse> RequestAsyncExecutor<TResponse>(IAsyncRequest<TResponse> request)
            => _executor.ApiAction(() => this.SendAsync(request), request, CreateException);

        private Exception CreateException(string s, Exception exception) => new UnhandledUserException(s, exception);

        private static bool IsSyncWsUrl(string par)
            => par.StartsWith("syncws://") && !par.Contains("?launch=1");
    }
}