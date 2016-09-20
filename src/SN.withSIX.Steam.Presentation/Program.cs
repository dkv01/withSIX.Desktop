// <copyright company="SIX Networks GmbH" file="Program.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
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
                Common.Flags = new Common.StartupFlags(args, Environment.Is64BitOperatingSystem);
                SetupNlog.Initialize("SteamHelper");
                Environment.Exit(new CommandRunner(BuildCommands()).RunCommandsAndLog(args));
            } catch (SteamNotFoundException ex) {
                Error(ex, 4);
            } catch (SteamInitializationException ex) {
                Error(ex, 3);
            } catch (TimeoutException ex) {
                Error(ex, 9);
            } catch (OperationCanceledException ex) {
                Error(ex, 10);
            } catch (Exception ex) {
                Error(ex, 1);
            } catch {
                Error("Native code exception!", 2);
            }
        }

        private static void Error(Exception ex, int exitCode) {
            var formatted = ex.Format();
            MainLog.Logger.Error(formatted);
            Console.Error.WriteLine(
#if DEBUG
                formatted
#else
                ex.Message
#endif
                );
            Environment.Exit(exitCode);
        }

        private static void Error(string msg, int exitCode) {
            MainLog.Logger.Error(msg);
            Console.Error.WriteLine(msg);
            Environment.Exit(exitCode);
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