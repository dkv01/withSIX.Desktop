// <copyright company="SIX Networks GmbH" file="Program.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using SN.withSIX.Steam.Api;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Steam.Presentation
{
    class Program
    {
        static void Main(string[] args) {
            var options = new Options {
                Force = args.Contains("--force")
            };
            var cla = args.Where(x => !x.StartsWith("--")).ToArray();
            try {
                Task.Factory.StartNew(
                    () => Process(Convert.ToUInt32(cla.First()), options, cla.Skip(1).ToArray()),
                    TaskCreationOptions.LongRunning)
                    .Unwrap()
                    .WaitAndUnwrapException();
            } catch (SteamInitializationException ex) {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(3);
            } catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
            } catch {
                Console.Error.WriteLine("Native code exception!");
                Environment.Exit(2);
            }
        }

        static async Task Process(uint appId, Options options, params string[] pIds) {
            using (await SteamSession.Start(appId).ConfigureAwait(false)) {
                var dl = new SteamDownloader();
                foreach (var nfo in pIds)
                    await ProcessContent(appId, options, nfo, dl).ConfigureAwait(false);
            }
        }

        private static async Task ProcessContent(uint appId, Options options, string nfo, ISteamDownloader dl) {
            ulong p;
            var force = false;
            if (nfo.StartsWith("!")) {
                force = true;
                p = Convert.ToUInt64(nfo.Substring(1));
            } else
                p = Convert.ToUInt64(nfo);

            Console.WriteLine($"Starting {p}");
            await
                dl.Download(appId, p, (l, d) => Console.WriteLine($"{l}/s {d}%"), force: options.Force || force)
                    .ConfigureAwait(false);
            Console.WriteLine($"Finished {p}");
        }

        class Options
        {
            public bool Force { get; set; }
        }
    }
}