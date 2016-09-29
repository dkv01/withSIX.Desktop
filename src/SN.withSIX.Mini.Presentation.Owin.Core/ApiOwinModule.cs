using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Usecases;
using SN.withSIX.Mini.Applications.Usecases.Main;
using SN.withSIX.Mini.Applications.Usecases.Main.Games;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Presentation.Owin.Core
{
    public class ApiOwinModule : OwinModule
    {
        public override void Configure(IApplicationBuilder app) {
            app.Map("/api", api => {
                api.Map("/version",
                    builder => builder.Run(context => context.RespondJson(new {Version = Consts.ApiVersion})));

                api.AddPath<PingPlugin>("/ping-plugin");

                api.Map("/external-downloads", content => {
                    content.AddPath<ExternalDownloadStarted, Guid>("/started");
                    content.AddPath<ExternalDownloadProgressing>("/progressing");
                    content.AddPath<AddExternalModRead>("/completed");
                    content.AddPath<StartDownloadSession>("/start-session");
                });

                api.Map("/content", content => {
                    content.AddPath<InstallContent>("/install-content");
                    content.AddPath<InstallContents>("/install-contents");
                    content.AddPath<InstallSteamContents>("/install-steam-contents");
                    content.AddPath<UninstallContent>("/uninstall-content");
                    content.AddPath<UninstallContents>("/uninstall-contents");
                    content.AddPath<LaunchContent>("/launch-content");
                    content.AddPath<LaunchContents>("/launch-contents");
                    content.AddPath<CloseGame>("/close-game");

                    // Deprecate
                    content.AddPath<AddExternalModRead>("/add-external-mod");
                });

                api.Map("/get-upload-folders",
                    builder =>
                        builder.Run(
                            context =>
                                context.ProcessRequest<List<string>, List<FolderInfo>>(
                                    folders => BuilderExtensions.Executor.SendAsync(new GetFolders(folders)))));

                api.Map("/whitelist-upload-folders",
                    builder =>
                        builder.Run(
                            context =>
                                context.ProcessRequest<List<string>>(
                                    folders => BuilderExtensions.Executor.SendAsync(new WhiteListFolders(folders)))));
            });

            app.Map("", builder => builder.Run(async ctx => ctx.Response.Redirect("https://withsix.com")));
        }
    }
}