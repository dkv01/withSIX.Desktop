// <copyright company="SIX Networks GmbH" file="Program.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using SN.withSIX.Core.Presentation.Logging;
using SN.withSIX.Mini.Presentation.Core;
using SN.withSIX.Mini.Presentation.Core.Commands;
using SN.withSIX.Steam.Api.Services;
using SN.withSIX.Steam.Presentation.Commands;

namespace SN.withSIX.Steam.Presentation
{
    class Program
    {
        static void Main(string[] args) {
            try {
                SetupNlog.Initialize("SteamHelper");
                Environment.Exit(new CommandRunner(BuildCommands()).RunCommandsAndLog(args));
            } catch (SteamInitializationException ex) {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(3);
            } catch (Exception ex) {
                Console.Error.WriteLine(
#if DEBUG
                    ex
#else
                ex.Message
#endif
                    );
                Environment.Exit(1);
            } catch {
                Console.Error.WriteLine("Native code exception!");
                Environment.Exit(2);
            }
        }

        private static IEnumerable<BaseCommand> BuildCommands() {
            var steamSessionFactory = new SteamSession.SteamSessionFactory();
            var steamApi = new SteamApi(steamSessionFactory);
            return new BaseCommand[] {
                new InstallCommand(steamSessionFactory, new SteamDownloader(steamApi), steamApi),
                new UninstallCommand(steamSessionFactory, steamApi)
            };
        }
    }
}