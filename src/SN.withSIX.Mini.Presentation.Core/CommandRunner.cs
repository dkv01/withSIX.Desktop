// <copyright company="SIX Networks GmbH" file="CommandRunner.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ManyConsole;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Logging;
using withSIX.Core.Presentation;
using withSIX.Mini.Presentation.Core.Commands;

namespace withSIX.Mini.Presentation.Core
{
    public class CommandRunner : IPresentationService
    {
        readonly IOrderedEnumerable<BaseCommand> _commands;

        public CommandRunner(IEnumerable<BaseCommand> commands) {
            _commands = commands.OrderBy(x => x.GetType().Name);
        }

        public int RunCommandsAndLog(string[] args) {
            MainLog.Logger.Info($"Starting with {args.CombineParameters()}");
            var r = RunCommands(args);
            if (r != 0)
                MainLog.Logger.Error("Error {0} dispatching command.", r);
            return r;
        }

        int RunCommands(string[] args) => ConsoleCommandDispatcher.DispatchCommand(_commands, args, new LogTextWriter());
    }

    public class LogTextWriter : TextWriter, IDisposable
    {
        private static readonly ILogger log = MainLog.Logger;

        public override void Flush() {
            if ((buffer != null) && (buffer.Length > 0)) {
                WriteLine();
            }
        }

        protected override void Dispose(bool disposing) {
            Flush();
            base.Dispose(disposing);
        }

        #region properties

        private StringBuilder buffer { get; }

        public override Encoding Encoding
        {
            get
            {
                // since this TextWrite is writing to log4net, we have no idea what the final encoding might be.
                // It all depends on the log4net configuration: tthe appender or appenders that wind up handling the logged message
                // determine the final encoding.
                //
                // Might make more sense to return Encoding.UTF8 though, just to return something.
                throw new NotImplementedException();
            }
        }

        #endregion properties ;

        #region public constructors

        public LogTextWriter() : this(null) {}

        public LogTextWriter(IFormatProvider formatProvider) : base(formatProvider) {
            buffer = new StringBuilder();
        }

        #endregion public constructors

        #region public Write() overloads

        public override void Write(bool value) {
            buffer.Append(value);
        }

        public override void Write(char value) {
            buffer.Append(value);
        }

        public override void Write(char[] buffer) {
            this.buffer.Append(buffer);
        }

        public override void Write(char[] buffer, int index, int count) {
            this.buffer.Append(buffer, index, count);
        }

        public override void Write(decimal value) {
            buffer.Append(value);
        }

        public override void Write(double value) {
            buffer.Append(value);
        }

        public override void Write(float value) {
            buffer.Append(value);
        }

        public override void Write(int value) {
            buffer.Append(value);
        }

        public override void Write(long value) {
            buffer.Append(value);
        }

        public override void Write(object value) {
            buffer.Append(value);
        }

        public override void Write(string format, object arg0) {
            buffer.AppendFormat(FormatProvider, format, arg0);
        }

        public override void Write(string format, object arg0, object arg1) {
            buffer.AppendFormat(FormatProvider, format, arg0, arg1);
        }

        public override void Write(string format, object arg0, object arg1, object arg2) {
            buffer.AppendFormat(FormatProvider, format, arg0, arg1, arg2);
        }

        public override void Write(string format, params object[] arg) {
            buffer.AppendFormat(FormatProvider, format, arg);
        }

        public override void Write(string value) {
            buffer.Append(value);
        }

        public override void Write(uint value) {
            buffer.Append(value);
        }

        public override void Write(ulong value) {
            buffer.Append(value);
        }

        public override void WriteLine() {
            var logMessage = buffer.ToString();

            buffer.Length = 0;
            log.Info(logMessage);
            Console.WriteLine(logMessage);
        }

        #endregion public Write() overloads

        #region public WriteLine() overloads

        public override void WriteLine(bool value) {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(char value) {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(char[] buffer) {
            Write(buffer);
            WriteLine();
        }

        public override void WriteLine(char[] buffer, int index, int count) {
            Write(buffer, index, count);
            WriteLine();
        }

        public override void WriteLine(decimal value) {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(double value) {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(float value) {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(int value) {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(long value) {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(object value) {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(string format, object arg0) {
            Write(format, arg0);
            WriteLine();
        }

        public override void WriteLine(string format, object arg0, object arg1) {
            Write(format, arg0, arg1);
            WriteLine();
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2) {
            Write(format, arg0, arg1, arg2);
            WriteLine();
        }

        public override void WriteLine(string format, params object[] arg) {
            Write(format, arg);
            WriteLine();
        }

        public override void WriteLine(string value) {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(uint value) {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(ulong value) {
            Write(value);
            WriteLine();
        }

        #endregion public WriteLine() overloads
    }
}