using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace withSIX.Core.Presentation.SA
{
    public class PrettyExceptions {
        private static readonly string[] separators = {"\r\n", "\n"};

        private static string AddPrefix(string msg, int level = 0) {
            if (level == 0)
                return msg;
            var prefix = new string(' ', level*4);
            return string.Join("\n", msg.Split((string[]) separators, StringSplitOptions.None).Select(x => prefix + x));
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
                AddPrefix($"Source: {e.Source}", level),
                AddPrefix($"TargetSite: {e.TargetSite}", level)
            };

            ProcessAdditionalExceptionInformation(e, str);

            if (e.Data != null && e.Data.Count > 0)
                str.Add(AddPrefix($"Data: {AddPrefix(PrettyPrint(e.Data), 1)}", level));

            if (e.StackTrace != null)
                str.Add(AddPrefix($"StackTrace:\n{AddPrefix(e.StackTrace, 1)}", level));

            ProcessAdditionalEmbeddingExceptionTypes(e, level, str);

            return string.Join("\n", str);
        }

        private static void ProcessAdditionalExceptionInformation(Exception e, ICollection<string> str) {
            var ee = e as ExternalException;
            if (ee != null)
                str.Add(AddPrefix($"ErrorCode: {ee.ErrorCode}"));

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
            if (rle != null
                && rle.LoaderExceptions != null) {
                str.AddRange(rle.LoaderExceptions.Select(
                    a => AddPrefix("Inner Exception:\n" + FormatException(a, 1), level)));
            }

            var ce = e as CompositionException;
            if (ce != null
                && ce.Errors != null) {
                str.AddRange(ce.Errors.Select(
                    error => AddPrefix(
                        $"CompositionError Description: {error.Description}, Element: {error.Element}, Exception: {(error.Exception == null ? null : FormatException(error.Exception, 1))}",
                        level)));
            }
        }
    }
}