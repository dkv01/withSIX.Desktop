using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using ShortBus;
using SN.withSIX.Api.Models;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Presentation.Core
{
    public class SIHandler : IUsecaseExecutor {
        private readonly Excecutor _executor = new Excecutor();

        public async Task<object> HandleSingleInstanceCall(List<string> parameters) {
            foreach (var p in parameters.Where(IsSyncWsUrl)) {
                var uri = new Uri(p);
                await ProcessSyncWsUrl(uri).ConfigureAwait(false);
            }
            return null;
        }

        private async Task ProcessSyncWsUrl(Uri uri) {
            var details = HttpUtility.ParseQueryString(uri.Query);
            var s = details["task"];
            var tasks = new[] { "launch" };
            if (s != null) tasks = s.Split(',');
            if (!tasks.Any()) {
                tasks = new[] { "launch" };
            }

            var gameId = GetGuid(uri.Host);
            var contentId = GetGuid(details["content"]);
            foreach (var t in tasks) {
                switch (t) {
                case "install": {
                    await RequestAsyncExecutor(new InstallContent(gameId, new ContentGuidSpec(contentId))).ConfigureAwait(false);
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
            => _executor.ApiAction<TResponse>(() => this.RequestAsync(request), request, CreateException);

        private Exception CreateException(string s, Exception exception) => new UnhandledUserException(s, exception);

        private static bool IsSyncWsUrl(string par)
            => par.StartsWith("syncws://") && !par.Contains("?launch=1");
    }
}