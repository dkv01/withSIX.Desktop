// <copyright company="SIX Networks GmbH" file="Helper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SN.withSIX.Mini.Presentation.Shell
{
    public class Helper
    {
        const int HttpsPort = 48666;
        //const int HttpPort = 48667;
        static readonly IPAddress SrvAddress = IPAddress.Parse("127.0.0.66");
        public static IPEndPoint HttpAddress = null; // new IPEndPoint(SrvAddress, HttpPort);
        public static IPEndPoint HttpsAddress = new IPEndPoint(SrvAddress, HttpsPort);
        public bool IsNotKnownToSync(string x) => !IsKnownToSync(x);
        // TODO: Probably should have a global registry per user account somewhere e.g in a json file instead?
        public bool IsKnownToSync(string x) => File.Exists(Path.Combine(x, ".sync.txt"));

        public async Task<List<FolderInfo>> TryGetInfo(List<string> folderPaths) {
            try {
                return await GetInfo(folderPaths).ConfigureAwait(false);
            } catch (Exception ex) {
                TryWriteLog("Error while TryGetInfo: " + ex);
                return new List<FolderInfo>();
            }
        }

        /*        public static void Try(Action fnc, string action)
        {
            try {
                fnc();
            } catch (Exception ex) {
                TryWriteLog("Error while trying action: " + fnc + ". " + ex);
            }
        }*/

        private static void TryWriteLog(string logMessage) {
            try {
                WriteLog(logMessage);
            } catch {}
        }


        private static void WriteLog(string logMessage) {
            File.WriteAllText(Assembly.GetExecutingAssembly().Location + ".log", logMessage);
        }

        public Task<List<FolderInfo>> GetInfo(List<string> folderPaths) {
            return PostJson<List<FolderInfo>>(folderPaths, GetUri("/api/get-upload-folders"));
        }

        private static Uri GetUri(string path) => new Uri("https://" + HttpsAddress + path);

        private static async Task<T> PostJson<T>(object obj, Uri uri) {
            using (var client = new HttpClient()) {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using (var content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8,
                    "application/json")) {
                    var r = await client.PostAsync(uri, content).ConfigureAwait(false);
                    r.EnsureSuccessStatusCode();
                    var c = await r.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<T>(c);
                }
            }
        }

        private static async Task PostJson(object obj, Uri uri) {
            using (var client = new HttpClient()) {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using (var content = new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8,
                    "application/json")) {
                    var r = await client.PostAsync(uri, content).ConfigureAwait(false);
                    r.EnsureSuccessStatusCode();
                }
            }
        }

        public void OpenUrl(string url) {
            OpenUrl(new Uri(url));
        }

        private void OpenUrl(Uri url) {
            Process.Start(url.ToString());
        }

        public async Task HandleSyncRunning() {
            // TODO: Show dialog??
            if (!await IsSyncRunning().ConfigureAwait(false))
                await StartSync().ConfigureAwait(false);
        }

        public Task<bool> IsSyncRunning() => Task.FromResult(true);

        public Task StartSync() {
            throw new NotImplementedException();
        }

        public Image LoadImage() {
            var assembly = Assembly.GetExecutingAssembly();
            var file = assembly.GetManifestResourceStream("SN.withSIX.Mini.Presentation.Shell." + "app.ico");
            return ResizeImage(Image.FromStream(file), 16, 16);
        }

        public static Bitmap ResizeImage(Image image, int width, int height) {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage)) {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public async Task Try(Func<Task> fnc, string action) {
            try {
                await fnc().ConfigureAwait(false);
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "Error while trying to: " + action);
            }
        }

        public Task WhitelistPaths(List<string> paths) => PostJson(paths, GetUri("/api/whitelist-upload-folders"));
    }

    public class ContentInfo
    {
        public ContentInfo(Guid userId, Guid gameId, Guid contentId) {
            UserId = userId;
            GameId = gameId;
            ContentId = contentId;
        }

        public Guid UserId { get; }
        public Guid GameId { get; }
        public Guid ContentId { get; }
    }

    public class FolderInfo
    {
        public FolderInfo(string path, ContentInfo contentInfo) {
            Path = path;
            ContentInfo = contentInfo;
        }

        public string Path { get; }
        public ContentInfo ContentInfo { get; set; }
    }

    public class ShortGuid
    {
        static readonly ShortGuid Default = new ShortGuid(Guid.Empty);
        readonly Guid _guid;
        readonly string _value;

        /// <summary>Create a 22-character case-sensitive short GUID.</summary>
        public ShortGuid(Guid guid) {
            if (guid == null)
                throw new ArgumentNullException(nameof(guid));

            _guid = guid;
            _value = Convert.ToBase64String(guid.ToByteArray())
                .Substring(0, 22)
                .Replace("/", "_")
                .Replace("+", "-");
        }

        /// <summary>Get the short GUID as a string.</summary>
        public override string ToString() => _value;

        /// <summary>Get the Guid object from which the short GUID was created.</summary>
        public Guid ToGuid() => _guid;

        /// <summary>Get a short GUID as a Guid object.</summary>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.FormatException"></exception>
        public static ShortGuid Parse(string shortGuid) {
            if (shortGuid == null)
                throw new ArgumentNullException(nameof(shortGuid));
            if (shortGuid.Length != 22)
                throw new FormatException("Input string was not in a correct format.");

            return new ShortGuid(new Guid(Convert.FromBase64String
                (shortGuid.Replace("_", "/").Replace("-", "+") + "==")));
        }

        public static bool TryParse(string shortGuid, out ShortGuid id) {
            try {
                id = Parse(shortGuid);
                return true;
            } catch {
                id = null;
                return false;
            }
        }

        public static ShortGuid ParseWithFallback(string shortGuid) {
            try {
                return Parse(shortGuid);
            } catch (ArgumentException) {
                return Default;
            } catch (FormatException) {
                return Default;
            }
        }

        public static implicit operator string(ShortGuid guid) => guid.ToString();

        public static implicit operator Guid(ShortGuid shortGuid) => shortGuid._guid;
    }
}