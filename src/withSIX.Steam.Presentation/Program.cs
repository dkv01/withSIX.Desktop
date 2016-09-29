// <copyright company="SIX Networks GmbH" file="Program.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;
using withSIX.Mini.Presentation.Core;
using withSIX.Steam.Core;
using withSIX.Steam.Presentation.Commands;

namespace withSIX.Steam.Presentation
{
    class Program
    {
        static void Main(string[] args) {
            try {
                Common.Flags = new Common.StartupFlags(args, Environment.Is64BitOperatingSystem);
                LoggingSetup.Setup("SteamHelper");
                using (var c = new ContainerSetup(() => RunInteractive.SteamApi)) {
                    Environment.Exit(new CommandRunner(c.GetCommands()).RunCommandsAndLog(args));
                }
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
    }
}