// <copyright company="SIX Networks GmbH" file="Program.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AutoMapper;
using ManyConsole;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Presentation.Wpf.Legacy;
using SN.withSIX.Sync.Presentation.Console.Commands;

namespace SN.withSIX.Sync.Presentation.Console
{
    class Program : IConsoleLauncher
    {
        const string AppName = "Synq";
        readonly IEnumerable<BaseCommand> _commands;
        MapperConfiguration _dummyForAutoMapper = new MapperConfiguration(cfg => { });

        public Program(IEnumerable<BaseCommand> commands) {
            _commands = commands.OrderBy(x => x.GetType().Name);
        }

        public int Run(params string[] args) {
            System.Console.WriteLine("Synq v" + Common.App.ApplicationVersion + " by SIX Networks");
            try {
                return ConsoleCommandDispatcher.DispatchCommand(_commands, args, System.Console.Out);
            } catch (Exception e) {
                FatalException(e);
                return 1;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FatalException(Exception e, string message = null) {
            //System.Console.WriteLine(message + "\n" + e);
            MainLog.Logger.FormattedErrorException(e, message);
            System.Console.WriteLine(e.Format());
        }

        [STAThread]
        static int Main(params string[] args) {
            try {
                StartupSequence.PreInit(AppName);
                return new AppBootstrapper().OnStartup(args);
            } catch (Exception e) {
                System.Console.WriteLine(e.Format());
                throw;
            }
        }
    }
}