// <copyright company="SIX Networks GmbH" file="ContentEngine.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SN.withSIX.ContentEngine.Core;
using SN.withSIX.ContentEngine.Infra.Services;
using SN.withSIX.Core.Infra.Services;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.ContentEngine.Infra
{
    public class ContentEngine : IContentEngine, IInfrastructureService
    {
        public enum Commands
        {
            installTeamspeakPlugin,
            installDll
        }

        public enum TsType
        {
            Any,
            x86,
            x64
        }

        const string Scripts_Folder = @"TSScripts";
        readonly ICEResourceService _resourceService;

        public ContentEngine(ICEResourceService resourceService) {
            _resourceService = resourceService;
        }

        public Task LoadModS(IContentEngineContent mod, IContentEngineGame game, bool overrideMod = false) {
            if (!ModHasScript(mod))
                throw new Exception("This mod does not have a Script");
            return LoadModSFromStream(mod, game);
        }

        public bool ModHasScript(IContentEngineContent mod) => ModHasScript(mod.NetworkId);

        public bool ModHasScript(Guid guid) => _resourceService.ResourceExists(GetScriptPath(guid));

        async Task LoadModSFromStream(IContentEngineContent content, IContentEngineGame game) {
            using (var streamR = new StreamReader(_resourceService.GetResource(GetScriptPath(content.Id)))) {
                var js = streamR.ReadToEnd();
                await HandleCommand(content, game, js).ConfigureAwait(false);
            }
        }

        private static async Task HandleCommand(IContentEngineContent content, IContentEngineGame game, string js) {
            var bc = js.FromJson<CommandBase>();
            switch (bc.Command) {
            case Commands.installDll: {
                var c = js.FromJson<InstallDllCommand>();
                InstallDll(content, game, c);
                break;
            }
            case Commands.installTeamspeakPlugin: {
                var c = js.FromJson<InstallTeamspeakPluginCommand>();
                InstallTeamspeakFiles(content, c);
                break;
            }
            }
        }

        private static void InstallTeamspeakFiles(IContentEngineContent content, InstallTeamspeakPluginCommand c) {
            var s = new TeamspeakService();
            foreach (var f in c.Files) {
                switch (f.Type) {
                case TsType.Any: {
                    foreach (var f2 in f.Source) {
                        var p = Path.Combine(content.PathInternal.ToString(), f2);
                        if (Directory.Exists(p)) {
                            if (s.IsX86Installed())
                                s.InstallX86PluginFolder(content, p, c.Options.Force);
                            if (s.IsX64Installed())
                                s.InstallX64PluginFolder(content, p, c.Options.Force);
                        } else {
                            if (s.IsX86Installed())
                                s.InstallX86Plugin(content, p, c.Options.Force);
                            if (s.IsX64Installed())
                                s.InstallX64Plugin(content, p, c.Options.Force);
                        }
                    }
                    break;
                }
                case TsType.x86: {
                    if (!s.IsX86Installed())
                        return;
                    foreach (var f2 in f.Source) {
                        var p = Path.Combine(content.PathInternal.ToString(), f2);
                        if (Directory.Exists(p)) {
                            s.InstallX86PluginFolder(content, p, c.Options.Force);
                        } else {
                            s.InstallX86Plugin(content, p, c.Options.Force);
                        }
                    }
                    break;
                }
                case TsType.x64: {
                    if (!s.IsX64Installed())
                        return;
                    foreach (var f2 in f.Source) {
                        var p = Path.Combine(content.PathInternal.ToString(), f2);
                        if (Directory.Exists(p)) {
                            s.InstallX64PluginFolder(content, p, c.Options.Force);
                        } else {
                            s.InstallX64Plugin(content, p, c.Options.Force);
                        }
                    }
                    break;
                }
                }
            }
        }

        private static void InstallDll(IContentEngineContent content, IContentEngineGame game, InstallDllCommand c) {
            var s = new GameFolderService();
            foreach (var f in c.Source)
                s.InstallDllPlugin(game, content, f, c.Options.Force);
        }

        static string GetScriptPath(Guid guid) => Scripts_Folder + @"\" + guid + ".js";

        class CommandBase
        {
            public Commands Command { get; set; }
        }

        class Options
        {
            public bool Force { get; set; }
        }

        class InstallDllCommand : CommandBase
        {
            public List<string> Source { get; set; }
            public Options Options { get; } = new Options();
        }

        class FileCommand
        {
            public List<string> Source { get; set; }
            public TsType Type { get; set; }
        }

        class InstallTeamspeakPluginCommand : CommandBase
        {
            public List<FileCommand> Files { get; set; }
            public Options Options { get; } = new Options();
        }
    }

    class ExpectedMod
    {
        internal ExpectedMod(Guid guid) {
            Guid = guid;
        }

        public Guid Guid { get; }
        public IModS Mod { get; private set; }

        internal void SetMod(IModS mod) {
            if (Mod != null)
                throw new ArgumentException("The registation for this mod has already been completed.", nameof(mod));
            Mod = mod;
        }
    }
}