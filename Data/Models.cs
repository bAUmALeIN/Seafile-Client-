using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;

namespace WinFormsApp3.Data
{
    public static class AppConfig
    {
        private const string DefaultApiUrl = "https://seafile.bbs-me.org/api2/";
        private const string DefaultLoginUrl = "https://bbs-me.org/app/Seafile/12";

        public static string ApiBaseUrl { get; set; } = DefaultApiUrl;
        public static string LoginUrl { get; set; } = DefaultLoginUrl;
        public static string DbConnectionString { get; } = "Data Source=seafile_data.db";
        public static string WebViewUserDataFolder { get; } = "WebView2_UserData";
        public static string CSRFToken { get; set; } = "";
        public static string RawCookies { get; set; } = "";

        public static class SettingsKeys
        {
            public const string ApiUrl = "api_url";
            public const string LoginUrl = "login_url";
            public const string ApiToken = "api_token";
        }
    }

    public class SeafileRepo
    {
        public string id { get; set; }
        public string name { get; set; }
        public long size { get; set; }
        public long mtime { get; set; }
        public string type { get; set; }
        public string owner { get; set; }
    }

    public class SeafileEntry
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public long size { get; set; }
        public long mtime { get; set; }
        public string parent_dir { get; set; }
    }

    public class DownloadItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; }
        public string Type { get; set; }
        public long TotalSize { get; set; }
        public long BytesTransferred { get; set; }
        public string Status { get; set; } = "Wartet...";
        public int Progress { get; set; } = 0;
        public string SpeedString { get; set; } = "-";
        public DateTime StartTime { get; set; } = DateTime.Now;
        public string ErrorMessage { get; set; }
        public string LocalFilePath { get; set; }
        public string RemotePath { get; set; }
        public object Tag { get; set; }

        // NEU: Liste der enthaltenen Dateien (für Ordner-Uploads)
        public List<TransferSubItem> SubItems { get; set; } = new List<TransferSubItem>();
    }

    public class TransferSubItem
    {
        public string Name { get; set; }
        public string Status { get; set; } = "Wartet"; // Wartet, Lädt, Fertig
    }

    public class SeafileShareLink
    {
        public string token { get; set; }
        public string link { get; set; }
        public int view_cnt { get; set; }
        public string expire_date { get; set; }
        public bool is_expired { get; set; }

        // WICHTIG: API liefert hier ein Objekt {}, keinen String!
        // Deswegen nutzen wir JToken (das schluckt alles: null, objekt, string)
        [JsonProperty("permissions")]
        public JToken PermissionsData { get; set; }

        public bool CanDownload
        {
            get
            {
                // Wenn null, erlauben wir es (Fallback)
                if (PermissionsData == null) return true;

                // Wir schauen direkt in das JToken
                try
                {
                    return PermissionsData["can_download"]?.ToObject<bool>() ?? true;
                }
                catch { return true; }
            }
        }
    }

    public class ManualMultipartContent : HttpContent
    {
        private readonly byte[] _head;
        private readonly Stream _fileStream;
        private readonly byte[] _tail;
        private readonly Action<long, long> _progress;
        private readonly long _totalSize;
        public ManualMultipartContent(byte[] head, Stream fileStream, byte[] tail, string boundary, Action<long, long> progress)
        {
            _head = head;
            _fileStream = fileStream;
            _tail = tail;
            _progress = progress;
            _totalSize = _head.Length + _fileStream.Length + _tail.Length;
            Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _totalSize;
            return true;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await stream.WriteAsync(_head, 0, _head.Length);
            byte[] buffer = new byte[8192];
            long bytesSent = _head.Length;
            int bytesRead;

            if (_fileStream.CanSeek) _fileStream.Position = 0;
            while ((bytesRead = await _fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
                bytesSent += bytesRead;
                _progress?.Invoke(bytesSent, _totalSize);
            }
            await stream.WriteAsync(_tail, 0, _tail.Length);
            _progress?.Invoke(_totalSize, _totalSize);
        }
    }



}