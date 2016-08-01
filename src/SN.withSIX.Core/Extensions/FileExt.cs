using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;

namespace SN.withSIX.Core.Extensions
{
    public static class FileExt
    {
        public static async Task<byte[]> ReadAllBytes(IAbsoluteFilePath fullPath) {
            Contract.Requires<ArgumentNullException>(fullPath != null);
            using (var fs = ReadFileStream(fullPath)) {
                var bytes = new byte[fs.Length];
                await fs.ReadAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                return bytes;
            }
        }

        public static async Task<byte[]> ReadAllBytes(IAbsoluteFilePath fullPath, CancellationToken token) {
            Contract.Requires<ArgumentNullException>(fullPath != null);
            Contract.Requires<ArgumentNullException>(token != null);

            using (var fs = ReadFileStream(fullPath)) {
                var bytes = new byte[fs.Length];
                await fs.ReadAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
                return bytes;
            }
        }

        static FileStream ReadFileStream(IAbsoluteFilePath fullPath)
            => new FileStream(fullPath.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read);

        public static Task<string> ReadAllText(IAbsoluteFilePath fullPath) {
            Contract.Requires<ArgumentNullException>(fullPath != null);
            return ReadAllText(fullPath, Encoding.UTF8);
        }

        public static async Task<string> ReadAllText(IAbsoluteFilePath fullPath, Encoding encoding) {
            Contract.Requires<ArgumentNullException>(fullPath != null);
            Contract.Requires<ArgumentNullException>(encoding != null);
            return encoding.GetString(await ReadAllBytes(fullPath).ConfigureAwait(false));
        }

        public static Task<string> ReadAllText(IAbsoluteFilePath fullPath, CancellationToken token) {
            Contract.Requires<ArgumentNullException>(fullPath != null);
            Contract.Requires<ArgumentNullException>(token != null);
            return ReadAllText(fullPath, Encoding.UTF8, token);
        }

        public static async Task<string> ReadAllText(IAbsoluteFilePath fullPath, Encoding encoding,
            CancellationToken token) {
            Contract.Requires<ArgumentNullException>(fullPath != null);
            Contract.Requires<ArgumentNullException>(encoding != null);
            Contract.Requires<ArgumentNullException>(token != null);
            return encoding.GetString(await ReadAllBytes(fullPath, token).ConfigureAwait(false));
        }

        public static async Task WriteAllBytes(IAbsoluteFilePath fullPath, byte[] bytes) {
            Contract.Requires<ArgumentNullException>(fullPath != null);
            Contract.Requires<ArgumentNullException>(bytes != null);
            using (var fs = CreateFileStream(fullPath))
                await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        public static async Task WriteAllBytes(IAbsoluteFilePath fullPath, byte[] bytes, CancellationToken token) {
            Contract.Requires<ArgumentNullException>(fullPath != null);
            Contract.Requires<ArgumentNullException>(bytes != null);
            Contract.Requires<ArgumentNullException>(token != null);
            using (var fs = CreateFileStream(fullPath))
                await fs.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
        }

        static FileStream CreateFileStream(IAbsoluteFilePath fullPath)
            => new FileStream(fullPath.ToString(), FileMode.Create, FileAccess.Write, FileShare.Read);

        public static Task WriteAllText(IAbsoluteFilePath fullPath, string text)
            => WriteAllText(fullPath, text, Encoding.UTF8);

        public static Task WriteAllText(IAbsoluteFilePath fullPath, string text, Encoding encoding) {
            Contract.Requires<ArgumentNullException>(fullPath != null);
            Contract.Requires<ArgumentNullException>(text != null);
            Contract.Requires<ArgumentNullException>(encoding != null);
            return WriteAllBytes(fullPath, encoding.GetBytes(text));
        }

        public static Task WriteAllText(IAbsoluteFilePath fullPath, string text, CancellationToken token) {
            Contract.Requires<ArgumentNullException>(fullPath != null);
            Contract.Requires<ArgumentNullException>(text != null);
            Contract.Requires<ArgumentNullException>(token != null);
            return WriteAllText(fullPath, text, Encoding.UTF8, token);
        }

        public static Task WriteAllText(IAbsoluteFilePath fullPath, string text, Encoding encoding,
            CancellationToken token) {
            Contract.Requires<ArgumentNullException>(fullPath != null);
            Contract.Requires<ArgumentNullException>(text != null);
            Contract.Requires<ArgumentNullException>(encoding != null);
            Contract.Requires<ArgumentNullException>(token != null);
            return WriteAllBytes(fullPath, encoding.GetBytes(text), token);
        }

        public static Task<string> ReadText(this IAbsoluteFilePath fullPath) => ReadAllText(fullPath);

        public static Task<string> ReadText(this IAbsoluteFilePath fullPath, CancellationToken token)
            => ReadAllText(fullPath, token);

        public static Task<byte[]> ReadBytes(this IAbsoluteFilePath fullPath) => ReadAllBytes(fullPath);

        public static Task<byte[]> ReadBytes(this IAbsoluteFilePath fullPath, CancellationToken token)
            => ReadAllBytes(fullPath, token);

        public static Task WriteToFile(this byte[] bytes, IAbsoluteFilePath fullPath) => WriteAllBytes(fullPath, bytes);

        public static Task WriteToFile(this string text, IAbsoluteFilePath fullPath) => WriteAllText(fullPath, text);

        public static Task WriteToFile(this byte[] bytes, IAbsoluteFilePath fullPath, CancellationToken token)
            => WriteAllBytes(fullPath, bytes, token);

        public static Task WriteToFile(this string text, IAbsoluteFilePath fullPath, Encoding encoding)
            => WriteAllText(fullPath, text, encoding);

        public static Task WriteToFile(this string text, IAbsoluteFilePath fullPath, CancellationToken token)
            => WriteAllText(fullPath, text, token);

        public static Task WriteToFile(this string text, IAbsoluteFilePath fullPath, Encoding encoding,
            CancellationToken token) => WriteAllText(fullPath, text, encoding, token);
    }
}