// <copyright company="SIX Networks GmbH" file="ExceptionExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using withSIX.Core.Helpers;
using withSIX.Core.Logging;

namespace withSIX.Core.Extensions
{
    public static class ExceptionExtensions
    {
        public static readonly Func<Exception, int, string> FormatException = PrettyExceptions.FormatException;

        public static Exception UnwrapExceptionIfNeeded(this Exception ex)
            => ex is TargetInvocationException && (ex.InnerException != null) ? ex.InnerException : ex;

        public static bool IsElevationCancelled(this Win32Exception ex)
            => ex.NativeErrorCode == Win32ErrorCodes.ERROR_CANCELLED_ELEVATION;

        public static bool IsOutOfDiskspace(this Win32Exception ex)
            => ex.NativeErrorCode == Win32ErrorCodes.ERROR_OUT_OF_DISKSPACE;

        public static OperationCanceledException HandleUserCancelled(this Win32Exception ex) {
            MainLog.Logger.FormattedWarnException(ex, "User canceled elevation action");
            return new OperationCanceledException("User canceled elevation action", ex);
        }

        public static string Format(this Exception e, int level = 0) {
            Contract.Requires<ArgumentNullException>(e != null);

            return FormatException(e, level);
        }

        public class PrettyExceptions
        {
            private static readonly string[] separators = {"\r\n", "\n"};

            private static string AddPrefix(string msg, int level = 0) {
                if (level == 0)
                    return msg;
                var prefix = new string(' ', level*4);
                return string.Join("\n", msg.Split(separators, StringSplitOptions.None).Select(x => prefix + x));
            }

            private static string PrettyPrint(IDictionary dict) {
                if (dict == null)
                    return string.Empty;
                var dictStr = "[";
                var keys = dict.Keys;
                var i = 0;
                foreach (var key in keys) {
                    dictStr += key + "=" + dict[key];
                    if (i++ < keys.Count - 1)
                        dictStr += ", ";
                }
                return dictStr + "]";
            }

            public static string FormatException(Exception e, int level = 0) {
                if (e == null)
                    throw new ArgumentNullException(nameof(e), "Exception to format can't be null");
                var str = new List<string> {
                    AddPrefix($"Type: {e.GetType()}", level),
                    AddPrefix($"Message:\n{AddPrefix(e.Message, 1)}", level),
                    AddPrefix($"Source: {e.Source}", level)
                    //AddPrefix($"TargetSite: {e.TargetSite}", level)
                };

                ProcessAdditionalExceptionInformation(e, str);

                if ((e.Data != null) && (e.Data.Count > 0))
                    str.Add(AddPrefix($"Data: {AddPrefix(PrettyPrint(e.Data), 1)}", level));

                if (e.StackTrace != null)
                    str.Add(AddPrefix($"StackTrace:\n{AddPrefix(e.StackTrace, 1)}", level));

                ProcessAdditionalEmbeddingExceptionTypes(e, level, str);

                return string.Join("\n", str);
            }

            private static void ProcessAdditionalExceptionInformation(Exception e, ICollection<string> str) {
                //var ee = e as ExternalException;
                //if (ee != null)
                //  str.Add(AddPrefix($"ErrorCode: {ee.ErrorCode}"));

                var w32 = e as Win32Exception;
                if (w32 != null)
                    str.Add(AddPrefix($"NativeErrorCode: {w32.NativeErrorCode}"));
            }

            private static void ProcessAdditionalEmbeddingExceptionTypes(Exception e, int level, List<string> str) {
                var ae = e as AggregateException;
                if (ae != null) {
                    str.AddRange(ae.Flatten().InnerExceptions.Select(
                        a => AddPrefix("Inner Exception:\n" + FormatException(a, 1), level)));
                } else {
                    if (e.InnerException != null)
                        str.Add(AddPrefix("Inner Exception:\n" + FormatException(e.InnerException, 1), level));
                }

                var rle = e as ReflectionTypeLoadException;
                if ((rle != null)
                    && (rle.LoaderExceptions != null)) {
                    str.AddRange(rle.LoaderExceptions.Select(
                        a => AddPrefix("Inner Exception:\n" + FormatException(a, 1), level)));
                }
                /*
                                var ce = e as CompositionException;
                                if (ce != null
                                    && ce.Errors != null) {
                                    str.AddRange(ce.Errors.Select(
                                        error => AddPrefix(
                                            $"CompositionError Description: {error.Description}, Element: {error.Element}, Exception: {(error.Exception == null ? null : FormatException(error.Exception, 1))}",
                                            level)));
                                }*/
            }
        }
    }
}