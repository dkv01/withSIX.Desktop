// <copyright company="SIX Networks GmbH" file="ReactiveProcess.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using withSIX.Core.Extensions;
using ProcessExtensions = withSIX.Core.Services.Infrastructure.ProcessExtensions;

namespace withSIX.Core.Infra.Services
{
    /// <summary>
    ///     Uses character by character parsing for StandardOutput and StandardError, so \r can be processed.
    ///     Uses observables instead of eventhandlers. Do not call BeginReadStandardOutput and BeginReadStandardError etc
    /// </summary>
    public class ReactiveProcess : Process
    {
        readonly CompositeDisposable _observables;
        readonly Subject<string> _standardErrorObservable;
        readonly Subject<string> _standardOutputObservable;

        public ReactiveProcess() {
            _standardOutputObservable = new Subject<string>();
            _standardErrorObservable = new Subject<string>();
            _observables = new CompositeDisposable {_standardOutputObservable, _standardErrorObservable};
        }

        public IObservable<string> StandardOutputObservable => _standardOutputObservable.AsObservable();
        public IObservable<string> StandardErrorObservable => _standardErrorObservable.AsObservable();

        public static ReactiveProcess Create(string fileName, string arguments)
            => new ReactiveProcess {StartInfo = new ProcessStartInfo(fileName, arguments)};

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing)
                _observables.Dispose();
        }

        /// <summary>
        ///     Validates StartInfo. Completes once the process has exited
        /// </summary>
        /// <returns></returns>
        public Task StartAsync() {
            this.Validate();
            Start();
            return Task.WhenAll(CreateAfterLaunchTasks());
        }

        IEnumerable<Task> CreateAfterLaunchTasks() {
            var tasks = new List<Task>();
            if (StartInfo.RedirectStandardOutput)
                tasks.Add(ReadStreamToEnd(StandardOutput, _standardOutputObservable));
            if (StartInfo.RedirectStandardError)
                tasks.Add(ReadStreamToEnd(StandardError, _standardErrorObservable));
            tasks.Add(ProcessExtensions.WaitForExitAsync(this));
            return tasks;
        }

        static async Task ReadStreamToEnd(StreamReader stream, IObserver<string> observable) {
            try {
                var readBuffer = new char[1];
                var lineBuffer = new StringBuilder();
                while (await stream.ReadAsync(readBuffer, 0, 1).ConfigureAwait(false) > 0) {
                    var c = readBuffer[0];
                    lineBuffer.Append(c);
                    // This does not account for unterminated lines.... like 'verifying download...'
                    // We would be able to do this by sending the data also byte by byte to the receivers, but for little gain.
                    if ((c != '\r') && (c != '\n'))
                        continue;
                    observable.OnNext(lineBuffer.ToString());
                    lineBuffer.Clear();
                }

                if (lineBuffer.Length > 0)
                    observable.OnNext(lineBuffer.ToString());
                observable.OnCompleted();
            } catch (Exception ex) {
                observable.OnError(ex);
                throw;
            }
        }
    }
}